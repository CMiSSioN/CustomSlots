using Harmony;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BattleTech;
using HBS.Logging;
using HBS.Util;


namespace CustomSlots
{


    public class Control
    {
        private string prefix = "[CSl]";


        private static Control _instance;

        public static Control Instance => _instance ?? (_instance = new Control());



        public CustomSlotsSettings Settings = new CustomSlotsSettings();

        private static ILog Logger;
        private static FileLogAppender logAppender;

        public static void Init(string directory, string settingsJSON)
        {
            Instance.InitNonStatic(directory, settingsJSON);
        }

        private void InitNonStatic(string directory, string settingsJSON)
        {
            Logger = HBS.Logging.Logger.GetLogger("CustomSlots", LogLevel.Debug);

            try
            {
                try
                {
                    Settings = new CustomSlotsSettings();
                    JSONSerializationUtility.FromJSON(Settings, settingsJSON);
                    HBS.Logging.Logger.SetLoggerLevel(Logger.Name, Settings.LogLevel);
                }
                catch (Exception)
                {
                    Settings = new CustomSlotsSettings();
                }

                SetupLogging(directory);

                var harmony = HarmonyInstance.Create("io.github.denadan.CustomFilters");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                Logger.Log("Loaded CustomSlots v0.1 for bt 1.8");

                CustomComponents.Registry.RegisterSimpleCustomComponents(Assembly.GetExecutingAssembly());

                CustomComponents.Validator.RegisterClearInventory(HandHeldController.ClearInventory);
                CustomComponents.Validator.RegisterClearInventory(SpecialControler.ClearInventory);

                //CustomComponents.Validator.RegisterDropValidator( check: HandHeldController.PostValidator);
                CustomComponents.Validator.RegisterMechValidator(HandHeldController.ValidateMech, HandHeldController.CanBeFielded);
                CustomComponents.Validator.RegisterMechValidator(SpecialControler.ValidateMech, SpecialControler.CanBeFielded);
                CustomComponents.AutoFixer.Shared.RegisterMechFixer(HandHeldController.AutoFixMech);
                CustomComponents.AutoFixer.Shared.RegisterMechFixer(SpecialControler.AutoFixMech);


                Logger.LogDebug("done");
                Logger.LogDebug(JSONSerializationUtility.ToJSON(Settings));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        #region LOGGING
        [Conditional("CCDEBUG")]
        public void LogDebug(string message)
        {
            Logger.LogDebug(prefix + message);
        }
        [Conditional("CCDEBUG")]
        public void LogDebug(string message, Exception e)
        {
            Logger.LogDebug(prefix + message, e);
        }

        public void LogError(string message)
        {
            Logger.LogError(prefix + message);
        }
        public void LogError(string message, Exception e)
        {
            Logger.LogError(prefix + message, e);
        }
        public void LogError(Exception e)
        {
            LogError(prefix, e);
        }

        public void Log(string message)
        {
            Logger.Log(prefix + message);
        }



        internal void SetupLogging(string Directory)
        {
            var logFilePath = Path.Combine(Directory, "log.txt");

            try
            {
                ShutdownLogging();
                AddLogFileForLogger(logFilePath);
            }
            catch (Exception e)
            {
                Logger.Log("HandHeld: can't create log file", e);
            }
        }

        internal void ShutdownLogging()
        {
            if (logAppender == null)
            {
                return;
            }

            try
            {
                HBS.Logging.Logger.ClearAppender("HandHeld");
                logAppender.Flush();
                logAppender.Close();
            }
            catch
            {
            }

            logAppender = null;
        }

        private void AddLogFileForLogger(string logFilePath)
        {
            try
            {
                logAppender = new FileLogAppender(logFilePath, FileLogAppender.WriteMode.INSTANT);
                HBS.Logging.Logger.AddAppender("HandHeld", logAppender);

            }
            catch (Exception e)
            {
                Logger.Log("HandHeld: can't create log file", e);
            }
        }

        #endregion

    }
}
