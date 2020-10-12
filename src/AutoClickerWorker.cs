using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualAutoClicker
{
    public class AutoClickerWorker
    {
        public Dictionary<string, AutoClicker> AutoClickers { get; set; }

        public AutoClickerWorker()
        {
            AutoClickers = new Dictionary<string, AutoClicker>();
        }

        /// <summary>
        /// Stops and nulls worker's autoclicker if active
        /// </summary>
        public void Picnic()
        {
            if (AutoClickers == null)
            {
                return;
            }

            foreach (var ac in AutoClickers)
            {
                ac.Value.Picnic();
                AutoClickers.Remove(ac.Key);
            }
        }

        /// <summary>
        /// Removes Autoclicker instance with given name
        /// </summary>
        public void RemoveAc(string key)
        {
            try
            {
                AutoClickers?.Remove(key);
            } 
            catch (Exception exc)
            {
                ConsoleHelper.WriteError($"Failed to remove Autoclicker '{key}'", exc);
            }
        }

        /// <summary>
        /// Returns a formatted string with all autoclicker's and their status
        /// </summary>
        /// <returns></returns>
        public string GetAutoClickerStatusString()
        {
            if (AutoClickers != null && AutoClickers.Count > 0)
            {
                var result = new StringBuilder();
                result.Append("List of all started autoclickers:\n\r");

                var acLastInList = AutoClickers.ElementAt(AutoClickers.Count - 1).Key;

                foreach (var ac in AutoClickers)
                {
                    result.Append($"  * {ac.Key} - {(ac.Value.Active ? "Running" : "Paused")}");
                    if (ac.Key != acLastInList)
                    {
                        result.Append("\r\n");
                    }
                }

                return result.ToString();
            }

            return "No running autoclickers!";
        }
    }
}
