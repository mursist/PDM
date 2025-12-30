using System;
using System.Runtime.InteropServices;
using BomEtlAddin.Utilities;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace BomEtlAddin
{
    [Guid("9A7F3E2C-23E4-4CB5-9F8A-3B7D2D468A72")]
    [ComVisible(true)]
    public class Addin : ISwAddin
    {
        private const int CommandGroupId = 5;
        private const int CommandIdExportBom = 1;

        private ISldWorks _swApp;
        private ICommandManager _commandManager;
        private int _addinId;

        public bool ConnectToSW(object thisSW, int cookie)
        {
            _swApp = (ISldWorks)thisSW;
            _addinId = cookie;
            _swApp.SetAddinCallbackInfo2(0, this, cookie);
            SetupCommands();
            return true;
        }

        public bool DisconnectFromSW()
        {
            RemoveCommands();
            _swApp = null;
            return true;
        }

        private void SetupCommands()
        {
            _commandManager = _swApp.GetCommandManager(_addinId);
            var cmdGroup = _commandManager.CreateCommandGroup2(
                CommandGroupId,
                "BOM ETL",
                "Export BOM to CSV",
                "",
                -1,
                false,
                out _);

            cmdGroup.AddCommandItem2(
                "Export BOM",
                -1,
                "Export active document BOM to CSV",
                "Export BOM",
                0,
                nameof(ExportBomCallback),
                string.Empty,
                CommandIdExportBom,
                (int)swCommandItemType_e.swMenuItem | (int)swCommandItemType_e.swToolbarItem);

            cmdGroup.HasMenu = true;
            cmdGroup.HasToolbar = true;
            cmdGroup.Activate();
        }

        private void RemoveCommands()
        {
            if (_commandManager != null)
            {
                _commandManager.RemoveCommandGroup(CommandGroupId);
                _commandManager = null;
            }
        }

        public void ExportBomCallback()
        {
            var model = _swApp?.ActiveDoc as IModelDoc2;
            if (model == null)
            {
                _swApp.SendMsgToUser2("Aktif bir doküman bulunamadı.",
                    (int)swMessageBoxIcon_e.swMbInformation,
                    (int)swMessageBoxBtn_e.swMbOk);
                return;
            }

            var exporter = new BomExporter(_swApp);
            var rows = exporter.GetActiveDocumentBom();
            if (rows.Count == 0)
            {
                _swApp.SendMsgToUser2("BOM okunamadı veya boş.",
                    (int)swMessageBoxIcon_e.swMbExclamation,
                    (int)swMessageBoxBtn_e.swMbOk);
                return;
            }

            var connectionString = Environment.GetEnvironmentVariable("BOM_ETL_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _swApp.SendMsgToUser2("BOM_ETL_CONNECTION_STRING ortam değişkeni tanımlı değil.",
                    (int)swMessageBoxIcon_e.swMbStop,
                    (int)swMessageBoxBtn_e.swMbOk);
                return;
            }

            SqlBomWriter.Write(connectionString, model.GetPathName(), rows);
            _swApp.SendMsgToUser2("BOM MSSQL veritabanına yazıldı.",
                (int)swMessageBoxIcon_e.swMbInformation,
                (int)swMessageBoxBtn_e.swMbOk);
            }
        }

        [ComRegisterFunction]
        public static void RegisterFunction(Type type)
        {
            var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey($@"SOFTWARE\SolidWorks\Addins\{type.GUID}");
            key.SetValue(null, 1);
            key.SetValue("Title", "BOM ETL Add-in");
            key.SetValue("Description", "BOM ETL Exporter");
            key.Close();
        }

        [ComUnregisterFunction]
        public static void UnregisterFunction(Type type)
        {
            Microsoft.Win32.Registry.LocalMachine.DeleteSubKey($@"SOFTWARE\SolidWorks\Addins\{type.GUID}", false);
        }
    }
}
