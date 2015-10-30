using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nancy.LongPoll
{
  public class PollService : Nancy.LongPoll.IPollService
  {
    ILogger _Logger = null;

    public PollService(ILogger logger = null)
    {
      if (logger == null) logger = new EmptyLogger();

      _Logger = logger;
    }

    #region Settings
    private static int MAX_CLIENTS = 50000; // максимальное число клиентов
    private static int CLIENT_TIMEOUT = 600; // сек - если в течение этого времени клиент ни разу не подключался, он считается отключенным 
    #endregion

    #region Logic
    internal class Message
    {
      public class RetCodes
      {
        public const string Ok = "Ok"; // доставлено сообщение, клиент может продолжать опрос
        public const string ClientsMaximumExceeded = "NoFreeConnections"; // клиент должен попытаться переподключиться
        public const string ErrorOnServer = "ErrorOnServer"; // клиент должен попытаться переподключиться
        public const string StoppedByServer = "StoppedByServer"; // клиент не должен пытаться переподключиться - сервер отбросил лишний дублированный вызов
        public const string ClientNotRegistered = "ClientNotRegistered"; // клиент должен попытаться зарегистрироваться и продолжить опрос
        public const string NextRequest = "NextRequest"; // вызов просрочен - клиент должен попытаться переподключиться
        public const string AlreadyRegistered = "AlreadyRegistered";
      }

      public bool success { get; set; }
      public string retcode { get; set; }
      public string messageName { get; set; }
      public string data { get; set; }

      public string GetJson()
      {
        return JsonConvert.SerializeObject(new
        {
          success = success,
          retcode = retcode,
          data = data,
          messageName = messageName
        });
      }
    }

    class Client
    {
      ILogger _Logger = null;
      public Client(ILogger logger)
      {
        if (logger == null) throw new ArgumentNullException("logger");

        _Logger = logger;
      }

      public string Id { get; set; }
      public string Ip { get; set; }
      public DateTime LastSeen { get; set; }

      #region Отправка сообщений клиенту
      public class ClientMessage : Message
      {
        public ClientMessage(Message message)
        {
          success = message.success;
          retcode = message.retcode;
          data = message.data;
          messageName = message.messageName;
        }

        public ulong sequenceCode { get; set; }

        public string GetJson()
        {
          return JsonConvert.SerializeObject(new
          {
            success = success,
            retcode = retcode,
            data = data,
            sequenceCode = sequenceCode,
            messageName = messageName
          });
        }
      }

      object m_SequenceLocker = new object();
      ulong m_CurrentSequenceNumber = 0;
      ulong GetNextSequenceNumber()
      {
        lock (m_SequenceLocker)
        {
          m_CurrentSequenceNumber++;
          return m_CurrentSequenceNumber;
        }
      }

      Queue<ClientMessage> m_MessageQueue = new Queue<ClientMessage>();
      public void SendMessage(Message message)
      {
        lock (m_MessageQueue)
        {
          m_MessageQueue.Enqueue(
                  new ClientMessage(message)
                  {
                    sequenceCode = GetNextSequenceNumber()
                  }
              );
        }
      }
      #endregion

      object m_RequestLocker = new object();
      ulong m_CurrentRequestId = 0;
      ulong GetNextRequestId()
      {
        lock (m_RequestLocker)
        {
          m_CurrentRequestId++;
          return m_CurrentRequestId;
        }
      }

      /// <param name="seqCode">Код последнего полученного сообщения</param>
      public Message Wait(ulong seqCode)
      {
        try
        {
          ulong requestId = GetNextRequestId();

          while (requestId == m_CurrentRequestId)
          {
            ClientMessage message = null;
            lock (m_MessageQueue)
            {
              if (m_MessageQueue.Count > 0)
              {
                while (true)
                {
                  if (m_MessageQueue.Count > 0
                      && m_MessageQueue.Peek().sequenceCode <= seqCode)
                  {
                    m_MessageQueue.Dequeue();
                  }
                  else
                  {
                    break;
                  }
                }

                if (m_MessageQueue.Count > 0) message = m_MessageQueue.Peek();
              }
            }
            if (message != null)
            {
              return message;
            }

            if ((DateTime.UtcNow - LastSeen).TotalSeconds >= 0.9 * CLIENT_TIMEOUT)
            {
              return new Message()
              {
                success = false,
                retcode = Message.RetCodes.NextRequest
              };
            }

            Thread.Sleep(100);
          }
        }
        catch (Exception ex)
        {
          _Logger.LogException(ex, "Client.Wait error");
          return new Message()
          {
            success = false,
            retcode = Message.RetCodes.ErrorOnServer
          };
        }

        return new Message
        {
          success = false,
          retcode = Message.RetCodes.StoppedByServer
        };
      }
    }

    private Dictionary<string, Client> _Clients = new Dictionary<string, Client>();
    private Dictionary<string, List<string>> _ClientIdToSessId = new Dictionary<string, List<string>>();
    private Dictionary<string, List<string>> _SessIdClientId = new Dictionary<string, List<string>>();

    private void RemoveSession(string sessId)
    {
      lock (_Clients)
      {
        if (_SessIdClientId.ContainsKey(sessId))
        {
          foreach (var clientId in _SessIdClientId[sessId])
          {
            _ClientIdToSessId[clientId].Remove(sessId);
            if (_ClientIdToSessId[clientId].Count == 0)
            {
              _ClientIdToSessId.Remove(clientId);
            }
          }
          _SessIdClientId.Remove(sessId);
        }
      }
    }

    private void RemoveClientId(string clientId)
    {
      lock (_Clients)
      {
        if (_ClientIdToSessId.ContainsKey(clientId))
        {
          foreach (var sessId in _ClientIdToSessId[clientId])
          {
            _SessIdClientId[sessId].Remove(clientId);
            if (_SessIdClientId[sessId].Count == 0)
            {
              _SessIdClientId.Remove(sessId);
            }
          }
          _ClientIdToSessId.Remove(clientId);
        }
      }
    }

    internal Message Register(string clientHostAddess, string sessionId)
    {
      try
      {
        lock (_Clients)
        {
          #region Отключение просроченных клиентов
          try
          {
            List<Client> clients = new List<Client>(_Clients.Values);
            foreach (var client in clients)
            {
              if ((DateTime.UtcNow - client.LastSeen).TotalSeconds > CLIENT_TIMEOUT)
              {
                RemoveClientId(client.Id);
                _Clients.Remove(client.Id);
              }
            }
          }
          catch (Exception ex)
          {
            _Logger.LogException(ex, "Exception on timeouted clients disconnection");
            return new Message()
            {
              success = false,
              retcode = Message.RetCodes.ErrorOnServer
            };
          }
          #endregion

          #region Проверка числа подключенных клиентов
          if (_Clients.Count > MAX_CLIENTS)
          {
            _Logger.LogWarn("Polling clients maximum exceeded");
            return new Message()
            {
              retcode = Message.RetCodes.ClientsMaximumExceeded,
              success = false
            };
          }
          #endregion

          string clientId = Guid.NewGuid().ToString("N");
          StopClient(clientId);

          _Clients.Add(clientId, new Client(_Logger)
          {
            Id = clientId,
            Ip = clientHostAddess,
            LastSeen = DateTime.UtcNow
          });
          if (!string.IsNullOrWhiteSpace(sessionId))
          {
            if (!_ClientIdToSessId.ContainsKey(clientId)) _ClientIdToSessId.Add(clientId, new List<string>());
            _ClientIdToSessId[clientId].Add(sessionId);
            if (!_SessIdClientId.ContainsKey(sessionId)) _SessIdClientId.Add(sessionId, new List<string>());
            _SessIdClientId[sessionId].Add(clientId);
          }

          return new Message()
          {
            success = true,
            retcode = Message.RetCodes.Ok,
            data = clientId
          };
        }
      }
      catch (Exception ex)
      {
        _Logger.LogException(ex, "Exception on Register");
        return new Message()
        {
          success = false,
          retcode = "Server internal error on register client"
        };
      }
    }

    internal string Wait(string clientId, ulong seqCode)
    {
      try
      {
        Client client = null;
        lock (_Clients)
        {
          if (_Clients.ContainsKey(clientId))
          {
            client = _Clients[clientId];
            client.LastSeen = DateTime.UtcNow;
          }
        }
        if (client != null)
        {
          Message message = client.Wait(seqCode);
          if (message is Client.ClientMessage)
          {
            return (message as Client.ClientMessage).GetJson();
          }

          return message.GetJson();
        }
        else
        {
          return new Message
          {
            success = false,
            retcode = Message.RetCodes.ClientNotRegistered
          }.GetJson();
        }
      }
      catch (Exception ex)
      {
        _Logger.LogException(ex, "Exception on Poll.Wait");
        return new Message
        {
          success = false,
          retcode = Message.RetCodes.ErrorOnServer
        }.GetJson();
      }
    }
    #endregion

    #region Interface methods
    public void StopClient(string clientId)
    {
      try
      {
        lock (_Clients)
        {
          if (_Clients.ContainsKey(clientId))
          {
            _Clients[clientId].SendMessage(new Message()
            {
              success = false,
              retcode = Message.RetCodes.StoppedByServer
            });
            RemoveClientId(clientId);
            _Clients.Remove(clientId);
          }
        }
      }
      catch (Exception ex)
      {
        _Logger.LogException(ex, "Exception on StopClient");
      }
    }

    public void SendMessageToAllClients(string messageName, string message)
    {
      try
      {
        lock (_Clients)
        {
          SendMessage(_Clients.Keys.ToList(), messageName, message);
        }
      }
      catch (Exception ex)
      {
        _Logger.LogException(ex, "Error sending message '" + messageName + "' to all clients. Message: " + message);
      }
    }

    public void SendMessage(string clientId, string messageName, string message)
    {
      SendMessage(new List<string>() { clientId }, messageName, message);
    }

    public void SendMessage(List<string> clientIds, string messageName, string message)
    {
      if (clientIds == null) return;

      try
      {
        lock (_Clients)
        {
          clientIds = clientIds.ToList();
          foreach (var clientId in clientIds)
          {
            if (_Clients.ContainsKey(clientId))
            {
              var client = _Clients[clientId];
              if ((DateTime.UtcNow - client.LastSeen).TotalSeconds > CLIENT_TIMEOUT)
              {
                RemoveClientId(client.Id);
                _Clients.Remove(client.Id);
                continue;
              }

              client.SendMessage(new Message()
              {
                success = true,
                retcode = Message.RetCodes.Ok,
                messageName = messageName,
                data = message
              });
            }
          }
        }
      }
      catch (Exception ex)
      {
        _Logger.LogException(ex, "Error sending message '" + messageName + "' to clients. Message: " + message);
      }
    }

    public void SendMessageToSession(string sessId, string messageName, string message)
    {
      try
      {
        lock (_Clients)
        {
          if (_SessIdClientId.ContainsKey(sessId))
          {
            SendMessage(_SessIdClientId[sessId], messageName, message);
          }
        }
      }
      catch (Exception ex)
      {
        _Logger.LogException(ex, "Exception in SendMessageToSession: sessId=" + sessId);
      }
    }

    public void SendMessageToSessions(List<string> sessIds, string messageName, string message)
    {
      if (sessIds == null) return;
      try
      {
        lock (_Clients)
        {
          sessIds = sessIds.ToList();
          List<string> clientIds = new List<string>();
          foreach (var sessId in sessIds)
          {
            if (_SessIdClientId.ContainsKey(sessId))
            {
              clientIds.AddRange(_SessIdClientId[sessId]);
            }
          }
          SendMessage(clientIds, messageName, message);
        }
      }
      catch (Exception ex)
      {
        _Logger.LogException(ex, "Exception in SendMessageToSessions");
      }
    }
    #endregion
  }

  interface IPollService
  {
    void SendMessage(System.Collections.Generic.List<string> clientIds, string messageName, string message);
    void SendMessage(string clientId, string messageName, string message);
    void SendMessageToAllClients(string messageName, string message);
    void SendMessageToSession(string sessId, string messageName, string message);
    void SendMessageToSessions(System.Collections.Generic.List<string> sessIds, string messageName, string message);
    void StopClient(string clientId);
  }
}
