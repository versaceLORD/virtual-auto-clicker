using System;

namespace VirtualAutoClicker.Commands
{
    public interface ICommand : IDisposable
    {
        public void Init();


    }
}
