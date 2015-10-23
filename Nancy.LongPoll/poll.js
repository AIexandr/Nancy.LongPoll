var isPollActive = false;
var isPollConnected = false;

var pollEvent = function (data) {
}

var pollConnected = function () {
}

var pollDisconnected = function () {
}

var clientId = "";
var currentAjaxRequest = null;
var startPoll = function () {
  if (isPollConnected) return;
  isPollActive = true;
  currentAjaxRequest = $.ajax({
    url: "/Poll/Register",
    context: document.body,
    async: true,
    success: function (res) {
      if (res.success) {
        clientId = res.data;
        if (!isPollConnected) {
          isPollConnected = true;
          try {
            pollConnected();
          } catch (e) { }
        }

        poll();
      }
      else {
        if (isPollConnected) {
          isPollConnected = false;
          try {
            pollDisconnected();
          } catch (e) { }
        }
        if (isPollActive) {
          setTimeout(function () {
            startPoll();
          }, 2000);
        }
      }
    },
    error: function (res) {
      if (isPollConnected) {
        isPollConnected = false;
        try {
          pollDisconnected();
        } catch (e) { }
      }
      if (isPollActive) {
        setTimeout(function () {
          startPoll();
        }, 2000);
      }
    }
  });
}

var stopPoll = function () {
  isPollActive = false;
  seqCode = 0;
  clientId = "";
  if (currentAjaxRequest != null) {
    currentAjaxRequest.abort();
  }
}

var seqCode = 0;
var poll = function () {
  currentAjaxRequest = $.ajax({
    url: "/Poll/Wait/?clientId=" + clientId + "&seqCode=" + seqCode,
    async: true,
    context: document.body,
    success: function (res) {
      if (!isPollActive) {
        isPollConnected = false;
        try {
          pollDisconnected();
        } catch (e) { }
      } else {
        if (!isPollConnected) {
          isPollConnected = true;
          try {
            pollConnected();
          } catch (e) { }
        }
      }
      try {
        res = JSON.parse(res);
      } catch (e) {
        res = { success: false };
      }
      if (res.success) {
        if (isPollActive) {
          try {
            pollEvent(res.messageName, res.data);
          } catch (e) { }
          try {
            var fnName = res.messageName + "Handler"
            var fnCall = "if(" + fnName + " != null)" + fnName + "('" + res.data + "');";
            eval(fnCall);
          } catch (e) { }
          seqCode = res.sequenceCode;
          poll();
        }
      }
      else {
        if (res.retcode == "StoppedByServer") {
          if (isPollConnected) {
            isPollConnected = false;
            try {
              pollDisconnected();
            } catch (e) { }
          }
          return;
        }
        else
          if (res.retcode == "ClientNotRegistered") {
            if (isPollConnected) {
              isPollConnected = false;
              try {
                pollDisconnected();
              } catch (e) { }
            }
            startPoll();
          } else {
            if (isPollActive) {
              poll();
            }
          }
      }
    },
    error: function (res) {
      if (isPollConnected) {
        isPollConnected = false;
        try {
          pollDisconnected();
        } catch (e) { }
      }
      if (isPollActive) {
        setTimeout(function () {
          startPoll();
        }, 3000);
      }
    }
  });
}