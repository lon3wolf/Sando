using System.Collections.Generic;
using System.IO;

namespace Configuration.OptionsPages
{
	public class SandoOptions
	{
		public SandoOptions(string extensionPointsPluginDirectoryPath, int numberOfSearchResultsReturned, 
                            bool allowDataCollectionLogging, List<string> fileExtensionsToIndex)
		{
			ExtensionPointsPluginDirectoryPath = extensionPointsPluginDirectoryPath;
            ExtensionPointsConfigurationFilePath = Path.Combine(extensionPointsPluginDirectoryPath, "ExtensionPointsConfiguration.xml");
            NumberOfSearchResultsReturned = numberOfSearchResultsReturned;
			AllowDataCollectionLogging = allowDataCollectionLogging;
		    FileExtensionsToIndex = fileExtensionsToIndex;
		}

		public string ExtensionPointsPluginDirectoryPath { get; protected set; }
        public string ExtensionPointsConfigurationFilePath { get; protected set; }
		public int NumberOfSearchResultsReturned { get; protected set; }
		public bool AllowDataCollectionLogging { get; protected set; }
        public List<string> FileExtensionsToIndex  { get; protected set; }
	}
}
