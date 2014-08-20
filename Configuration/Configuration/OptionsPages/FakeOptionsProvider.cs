using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Configuration.OptionsPages
{
    public class FakeOptionsProvider : ISandoOptionsProvider
    {
        private string _myIndex;
        private int _myResultsNumber;
        private bool _myAllowLogs;
        private List<string> _myFileExtensions;

        public FakeOptionsProvider(string index, int num, bool allowLogs, List<string> fileExtensions)
        {
            _myIndex = index;
            _myResultsNumber = num;
            _myAllowLogs = allowLogs;
            _myFileExtensions = SandoOptionsControl.DefaultFileExtensionsList;
        }

        public SandoOptions GetSandoOptions()
        {
            return new SandoOptions(_myIndex, _myResultsNumber, _myAllowLogs, _myFileExtensions);
        }
    }
}
