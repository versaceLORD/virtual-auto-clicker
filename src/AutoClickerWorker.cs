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
            if (AutoClickers is null)
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
            if (AutoClickers is { } && AutoClickers.Count > 0)
            {
                return string.Join(
                    "\n",
                    AutoClickers.Select(x => $"  * {x.Key} - {(x.Value.Active ? "Running" : "Paused")}")
                    .Prepend("List of all started autoclickers:")
                );
            }

            return "No running autoclickers!";
        }
    }
}
