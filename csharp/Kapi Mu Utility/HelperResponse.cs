using System;
using System.Collections.Generic;
using System.Text;

namespace Kapi_Mu_Utility
{
    public enum ActionType
    {
        Error = 0,
        Trade = 1
    }

    public class HelperResponse
    {
        public ActionType IdAction { get; set; }
        public string Action { get; set; }
        public string Message { get; set; }
        public Point? Cursor { get; set; }

        public HelperResponse(
            ActionType idAction,
            string message = "",
            Point? cursor = null)
        {
            IdAction = idAction;
            Action = idAction.ToString();
            Message = message;
            Cursor = cursor;
        }

        public class AccionOCR
        {
            public ActionType Tipo { get; set; }
            public Point? Cursor { get; set; }
            public bool ClickDerecho { get; set; }
            public int DuracionMs { get; set; } = 200;
            public int PausaMs { get; set; } = 0;
        }
    }
}
