using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtualAutoClicker
{
    public class AutoClickerWorker
    {
        public Dictionary<string, AutoClicker> AutoClickers { get; }

        public AutoClickerWorker()
        {
            AutoClickers = new Dictionary<string, AutoClicker>();
        }
        
        /// <summary>
        /// Retrieves an autoclicker with given name
        /// </summary>
        /// <param name="acName"></param>
        /// <returns></returns>
        public AutoClicker? GetAutoclicker(string acName)
        {
            var acWorker = VacEnvironment.GetAcWorker();
            var ac = acWorker?.AutoClickers.FirstOrDefault(a => a.Value.Name == acName).Value;
            if (ac is null)
            {
                ConsoleHelper.WriteWarning($"Couldn't find an autoclicker named '{acName}'. Use command 'list' to see all running autoclickers.");
            }

            return ac;
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
