using System;
namespace KL.AzureBlobSync
{
    /// <summary>
    /// Local folder synchronizer
    /// </summary>
    public class LocalFolderSynchronizerFactory
    {
        /// <summary>
        /// Sync the folder to itself
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="syncFilePath"></param>
        /// <returns></returns>
        public IFolderSynchronizer CreateSelfFolderSynchronizer(string folder, string syncFilePath)
        {
            return new SelfLocalSynchronizer(folder, syncFilePath);
        }
    }
}
