using DotNet3dsToolkit.Ctr;
using SkyEditor.Core.IO;
using SkyEditor.Core.IO.PluginInfrastructure;
using SkyEditor.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotNet3dsToolkit
{
    public class ThreeDsRom : IOpenableFile, IOpenableFromGenericFile, IIOProvider, IDetectableFileType, IDisposable
    {
        private const int MediaUnitSize = 0x200;

        public static async Task<ThreeDsRom> Load(string filename)
        {
            var rom = new ThreeDsRom();
            await rom.OpenFile(filename);
            return rom;
        }

        public static async Task<ThreeDsRom> Load(string filename, IIOProvider provider)
        {
            var rom = new ThreeDsRom(provider);
            await rom.OpenFile(filename);
            return rom;
        }

        public ThreeDsRom()
        {
            (this as IIOProvider).ResetWorkingDirectory();
            CurrentIOProvider = new PhysicalIOProvider();
        }

        public ThreeDsRom(IIOProvider ioProvider)
        {
            CurrentIOProvider = ioProvider;
        }

        private INcchPartitionContainer Container { get; set; }

        public NcchPartition[] Partitions => Container.Partitions;

        private GenericFile RawData { get; set; }

        private IIOProvider CurrentIOProvider { get; set; }

        public async Task OpenFile(string filename, IIOProvider provider)
        {
            CurrentIOProvider = provider;

            if (provider.FileExists(filename))
            {
                RawData = new GenericFile();
                RawData.EnableMemoryMappedFileLoading = true;
                RawData.EnableInMemoryLoad = true;
                await RawData.OpenFile(filename, CurrentIOProvider);

                await OpenFile(RawData);
            }
            else if (provider.DirectoryExists(filename))
            {
                VirtualPath = filename;
                DisposeVirtualPath = false;
            }
            else
            {
                throw new FileNotFoundException("Could not find file or directory at the given path", filename);
            }
        }

        public async Task OpenFile(GenericFile file)
        {
            // Clear virtual path if it exists
            if (!string.IsNullOrEmpty(VirtualPath) && CurrentIOProvider.DirectoryExists(VirtualPath))
            {
                CurrentIOProvider.DeleteDirectory(VirtualPath);
            }

            VirtualPath = CurrentIOProvider.GetTempDirectory();

            if (await NcsdFile.IsNcsd(file))
            {
                Container = await NcsdFile.Load(file);
            }
            else if (await CiaFile.IsCia(file))
            {
                Container = await CiaFile.Load(file);
            }
            else if (await NcchPartition.IsNcch(file))
            {
                Container = new SingleNcchPartitionContainer(await NcchPartition.Load(file));
            }
            else if (await RomFs.IsRomFs(file))
            {
                Container = new SingleNcchPartitionContainer(new NcchPartition(romfs: await RomFs.Load(file)));
            }
            else if (await ExeFs.IsExeFs(file))
            {
                Container = new SingleNcchPartitionContainer(new NcchPartition(exefs: await ExeFs.Load(file)));
            }
            else
            {
                throw new BadImageFormatException(Properties.Resources.ThreeDsRom_UnsupportedFileFormat);
            }
        }

        public async Task OpenFile(string filename)
        {
            await this.OpenFile(filename, CurrentIOProvider);
        }

        public NcchPartition GetPartitionOrDefault(int partitionIndex)
        {
            if (Partitions.Length > partitionIndex)
            {
                return Partitions[partitionIndex];
            }
            else
            {
                return null;
            }
        }

        public async Task ExtractFiles(string directoryName, IIOProvider provider, ProgressReportToken progressReportToken = null)
        {

            List<ExtractionProgressedToken> extractionProgressedTokens = null;
            if (progressReportToken != null)
            {
                extractionProgressedTokens = new List<ExtractionProgressedToken>();
                progressReportToken.IsIndeterminate = false;
            }

            void onExtractionTokenProgressed(object sender, EventArgs e)
            {
                if (progressReportToken != null)
                {
                    progressReportToken.Progress = (float)extractionProgressedTokens.Select(t => t.ExtractedFileCount).Sum() / extractionProgressedTokens.Select(t => t.TotalFileCount).Sum();
                }
            }

            if (!provider.DirectoryExists(directoryName))
            {
                provider.CreateDirectory(directoryName);
            }

            var tasks = new List<Task>();
            for (int i = 0; i < Partitions.Length; i++)
            {
                var partition = GetPartitionOrDefault(i);

                if (partition == null)
                {
                    continue;
                }

                if (partition.ExeFs != null)
                {
                    ExtractionProgressedToken exefsExtractionProgressedToken = null;
                    if (exefsExtractionProgressedToken != null)
                    {
                        exefsExtractionProgressedToken = new ExtractionProgressedToken();
                        exefsExtractionProgressedToken.FileCountChanged += onExtractionTokenProgressed;
                        extractionProgressedTokens.Add(exefsExtractionProgressedToken);
                    }
                    tasks.Add(partition.ExeFs.ExtractFiles(Path.Combine(directoryName, GetExeFsDirectoryName(i)), provider, exefsExtractionProgressedToken));
                }

                if (partition.ExHeader != null)
                {
                    ExtractionProgressedToken exefsExtractionProgressedToken = null;
                    if (exefsExtractionProgressedToken != null)
                    {
                        exefsExtractionProgressedToken = new ExtractionProgressedToken();
                        exefsExtractionProgressedToken.TotalFileCount = 1;
                        exefsExtractionProgressedToken.FileCountChanged += onExtractionTokenProgressed;
                        extractionProgressedTokens.Add(exefsExtractionProgressedToken);
                    }
                    tasks.Add(Task.Run(async () =>
                    {
                        File.WriteAllBytes(Path.Combine(directoryName, GetExHeaderFileName(i)), await partition.ExHeader.ReadAsync());
                        exefsExtractionProgressedToken?.IncrementExtractedFileCount();
                    }));
                }

                if (partition.RomFs != null)
                {
                    ExtractionProgressedToken romfsExtractionProgressedToken = null;
                    if (romfsExtractionProgressedToken != null)
                    {
                        romfsExtractionProgressedToken = new ExtractionProgressedToken();
                        romfsExtractionProgressedToken.FileCountChanged += onExtractionTokenProgressed;
                        extractionProgressedTokens.Add(romfsExtractionProgressedToken);
                    }

                    tasks.Add(partition.RomFs.ExtractFiles(Path.Combine(directoryName, GetRomFsDirectoryName(i)), provider, romfsExtractionProgressedToken));
                }

            }

            await Task.WhenAll(tasks);

            if (progressReportToken != null)
            {
                progressReportToken.Progress = 1;
                progressReportToken.IsCompleted = true;
            }
        }

        private string GetRomFsDirectoryName(int partitionId)
        {
            if (Container.IsDlcContainer)
            {
                return "RomFS-Partition-" + partitionId.ToString();
            }
            else
            {
                switch (partitionId)
                {
                    case 0:
                        return "RomFS";
                    case 1:
                        return "Manual";
                    case 2:
                        return "DownloadPlay";
                    case 6:
                        return "N3DSUpdate";
                    case 7:
                        return "O3DSUpdate";
                    default:
                        return "RomFS-Partition-" + partitionId.ToString();
                }
            }
        }

        private string GetExeFsDirectoryName(int partitionId)
        {
            switch (partitionId)
            {
                case 0:
                    return "ExeFS";
                default:
                    return "ExeFS-Partition-" + partitionId.ToString();
            }
        }

        private string GetExHeaderFileName(int partitionId)
        {
            switch (partitionId)
            {
                case 0:
                    return "ExHeader.bin";
                default:
                    return "ExHeader-" + partitionId.ToString() + ".bin";
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    RawData?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ThreeDsRom() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion

        #region IIOProvider Implementation
        /// <summary>
        /// Gets a regular expression for the given search pattern for use with <see cref="GetFiles(string, string, bool)"/>.  Do not provide asterisks.
        /// </summary>
        private static StringBuilder GetFileSearchRegexQuestionMarkOnly(string searchPattern)
        {
            var parts = searchPattern.Split('?');
            var regexString = new StringBuilder();
            foreach (var item in parts)
            {
                regexString.Append(Regex.Escape(item));
                if (item != parts[parts.Length - 1])
                {
                    regexString.Append(".?");
                }
            }
            return regexString;
        }

        /// <summary>
        /// Gets a regular expression for the given search pattern for use with <see cref="GetFiles(string, string, bool)"/>.
        /// </summary>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        private static string GetFileSearchRegex(string searchPattern)
        {
            var asteriskParts = searchPattern.Split('*');
            var regexString = new StringBuilder();

            foreach (var part in asteriskParts)
            {
                if (string.IsNullOrEmpty(part))
                {
                    // Asterisk
                    regexString.Append(".*");
                }
                else
                {
                    regexString.Append(GetFileSearchRegexQuestionMarkOnly(part));
                }
            }

            return regexString.ToString();
        }

        /// <summary>
        /// Keeps track of files that have been logically deleted
        /// </summary>
        private List<string> BlacklistedPaths => new List<string>();

        /// <summary>
        /// Path in the current I/O provider where temporary files are stored
        /// </summary>
        private string VirtualPath { get; set; }

        /// <summary>
        /// Whether or not to delete <see cref="VirtualPath"/> on delete
        /// </summary>
        private bool DisposeVirtualPath { get; set; }

        string IIOProvider.WorkingDirectory
        {
            get
            {
                var path = new StringBuilder();
                foreach (var item in _workingDirectoryParts)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        path.Append("/");
                        path.Append(item);
                    }
                }
                path.Append("/");
                return path.ToString();
            }
            set
            {
                _workingDirectoryParts = GetPathParts(value);
            }
        }
        private string[] _workingDirectoryParts;

        protected string[] GetPathParts(string path)
        {
            var parts = new List<string>();

            path = path.Replace('\\', '/');
            if (!path.StartsWith("/") && !(_workingDirectoryParts.Length == 1 && _workingDirectoryParts[0] == string.Empty))
            {
                parts.AddRange(_workingDirectoryParts);
            }

            foreach (var item in path.TrimStart('/').Split('/'))
            {
                switch (item)
                {
                    case "":
                    case ".":
                        break;
                    case "..":
                        parts.RemoveAt(parts.Count - 1);
                        break;
                    default:
                        parts.Add(item);
                        break;
                }
            }
            if (parts.Count == 0)
            {
                parts.Add(string.Empty);
            }
            return parts.ToArray();
        }

        void IIOProvider.ResetWorkingDirectory()
        {
            (this as IIOProvider).WorkingDirectory = "/";
        }

        private string FixPath(string path)
        {
            var fixedPath = path.Replace('\\', '/');

            // Apply working directory
            if (fixedPath.StartsWith("/"))
            {
                return fixedPath;
            }
            else
            {
                return Path.Combine((this as IIOProvider).WorkingDirectory, path);
            }
        }

        private string GetVirtualPath(string path)
        {
            return Path.Combine(VirtualPath, path.TrimStart('/'));
        }

        private IBinaryDataAccessor GetDataReference(string[] parts, bool throwIfNotFound = true)
        {
            IBinaryDataAccessor getExeFsDataReference(string[] pathParts, int partitionId)
            {
                if (pathParts.Length == 2)
                {
                    return GetPartitionOrDefault(partitionId)?.ExeFs?.GetDataReference(pathParts.Last());
                }

                return null;
            }

            IBinaryDataAccessor getRomFsDataReference(string[] pathParts, int partitionId)
            {
                var currentDirectory = Partitions[partitionId]?.RomFs?.Level3.RootDirectoryMetadataTable;
                for (int i = 1; i < pathParts.Length - 1; i += 1)
                {
                    currentDirectory = currentDirectory?.ChildDirectories.Where(x => string.Compare(x.Name, pathParts[i], true) == 0).FirstOrDefault();
                }
                if (currentDirectory != null)
                {
                    if (ReferenceEquals(currentDirectory, Partitions[partitionId].RomFs.Level3.RootDirectoryMetadataTable))
                    {
                        // The root RomFS directory doesn't contain files; those are located in the level 3
                        return GetPartitionOrDefault(partitionId).RomFs.Level3.RootFiles.FirstOrDefault(f => string.Compare(f.Name, pathParts.Last(), true) == 0)?.GetDataReference();
                    }
                    else
                    {
                        return currentDirectory.ChildFiles.FirstOrDefault(f => string.Compare(f.Name, pathParts.Last(), true) == 0)?.GetDataReference();
                    }

                }

                return null;
            }

            IBinaryDataAccessor dataReference = null;

            var firstDirectory = parts[0].ToLower();
            switch (firstDirectory)
            {
                case "exefs-partition-0":
                case "exefs":
                    dataReference = getExeFsDataReference(parts, 0);
                    break;
                case "romfs-partition-0":
                case "romfs":
                    dataReference = getRomFsDataReference(parts, 0);
                    break;
                case "romfs-partition-1":
                case "manual":
                    dataReference = getRomFsDataReference(parts, 1);
                    break;
                case "romfs-partition-2":
                case "downloadplay":
                    dataReference = getRomFsDataReference(parts, 2);
                    break;
                case "romfs-partition-6":
                case "n3dsupdate":
                    dataReference = getRomFsDataReference(parts, 6);
                    break;
                case "romfs-partition-7":
                case "o3dsupdate":
                    dataReference = getRomFsDataReference(parts, 7);
                    break;
                case "exheader.bin":
                    dataReference = GetPartitionOrDefault(0)?.ExHeader;
                    break;
                default:
                    if (firstDirectory.StartsWith("romfs-partition-"))
                    {
                        var partitionNumRaw = firstDirectory.Split("-".ToCharArray(), 3)[2];
                        if (int.TryParse(partitionNumRaw, out var partitionNum))
                        {
                            dataReference = getRomFsDataReference(parts, partitionNum);
                        }
                    }
                    else if (firstDirectory.StartsWith("exefs-partition-"))
                    {
                        var partitionNumRaw = firstDirectory.Split("-".ToCharArray(), 3)[2];
                        if (int.TryParse(partitionNumRaw, out var partitionNum))
                        {
                            dataReference = getExeFsDataReference(parts, partitionNum);
                        }
                    }
                    else if (firstDirectory.StartsWith("exheader-"))
                    {
                        var partitionNumRaw = firstDirectory.Split("-".ToCharArray(), 2)[1].Split(".".ToCharArray(), 2)[0];
                        if (int.TryParse(partitionNumRaw, out var partitionNum))
                        {
                            dataReference = GetPartitionOrDefault(partitionNum)?.ExHeader;
                        }
                    }
                    break;
            }

            if (dataReference != null)
            {
                return dataReference;
            }

            if (throwIfNotFound)
            {
                var path = "/" + string.Join("/", parts);
                throw new FileNotFoundException(string.Format(Properties.Resources.ThreeDsRom_ErrorRomFileNotFound, path), path);
            }
            else
            {
                return null;
            }
        }

        long IIOProvider.GetFileLength(string filename)
        {
            return GetDataReference(GetPathParts(filename)).Length;
        }

        bool IIOProvider.FileExists(string filename)
        {
            return (CurrentIOProvider != null && CurrentIOProvider.FileExists(GetVirtualPath(filename)))
                || GetDataReference(GetPathParts(filename), false) != null;
        }

        private bool DirectoryExists(string[] parts)
        {
            bool romfsDirectoryExists(string[] pathParts, int partitionId)
            {
                var currentDirectory = GetPartitionOrDefault(partitionId)?.RomFs?.Level3.RootDirectoryMetadataTable;
                for (int i = 1; i < pathParts.Length - 1; i += 1)
                {
                    currentDirectory = currentDirectory?.ChildDirectories.Where(x => string.Compare(x.Name, pathParts[i], true) == 0).FirstOrDefault();
                }
                return currentDirectory != null;
            }

            if (parts.Length == 1)
            {
                var dirName = parts[0].ToLower();
                switch (dirName)
                {
                    case "exefs-partition-0":
                    case "exefs":
                        return GetPartitionOrDefault(0)?.ExeFs != null;
                    case "romfs-partition-0":
                    case "romfs":
                        return GetPartitionOrDefault(0)?.RomFs != null;
                    case "romfs-partition-1":
                    case "manual":
                        return GetPartitionOrDefault(1)?.RomFs != null;
                    case "romfs-partition-2":
                    case "downloadplay":
                        return GetPartitionOrDefault(2)?.RomFs != null;
                    case "romfs-partition-6":
                    case "n3dsupdate":
                        return GetPartitionOrDefault(6)?.RomFs != null;
                    case "romfs-partition-7":
                    case "o3dsupdate":
                        return GetPartitionOrDefault(7)?.RomFs != null;
                    default:
                        if (dirName.StartsWith("romfs-partition-"))
                        {
                            var partitionNumRaw = dirName.Split("-".ToCharArray(), 3)[2];
                            if (int.TryParse(partitionNumRaw, out var partitionNum))
                            {
                                return GetPartitionOrDefault(partitionNum)?.RomFs != null;
                            }
                        }
                        else if (dirName.StartsWith("exefs-partition-"))
                        {
                            var partitionNumRaw = dirName.Split("-".ToCharArray(), 3)[2];
                            if (int.TryParse(partitionNumRaw, out var partitionNum))
                            {
                                return GetPartitionOrDefault(partitionNum)?.ExeFs != null;
                            }
                        }
                        return false;
                }
            }
            else if (parts.Length == 0)
            {
                throw new ArgumentException("Argument cannot be empty", nameof(parts));
            }
            else
            {
                var dirName = parts[0].ToLower();
                switch (dirName)
                {
                    case "exefs-partition-0":
                    case "exefs":
                        // Directories inside exefs are not supported
                        return false;
                    case "romfs-partition-0":
                    case "romfs":
                        return romfsDirectoryExists(parts, 0);
                    case "romfs-partition-1":
                    case "manual":
                        return romfsDirectoryExists(parts, 1);
                    case "romfs-partition-2":
                    case "downloadplay":
                        return romfsDirectoryExists(parts, 2);
                    case "romfs-partition-6":
                    case "n3dsupdate":
                        return romfsDirectoryExists(parts, 6);
                    case "romfs-partition-7":
                    case "o3dsupdate":
                        return romfsDirectoryExists(parts, 7);
                    default:
                        if (dirName.StartsWith("romfs-partition-"))
                        {
                            var partitionNumRaw = dirName.Split("-".ToCharArray(), 3)[2];
                            if (int.TryParse(partitionNumRaw, out var partitionNum))
                            {
                                return romfsDirectoryExists(parts, partitionNum);
                            }
                        }
                        else if (dirName.StartsWith("exefs-partition-"))
                        {
                            // Directories inside exefs are not supported
                            return false;
                        }
                        return false;
                }
            }
        }

        bool IIOProvider.DirectoryExists(string path)
        {
            return !BlacklistedPaths.Contains(FixPath(path))
                    &&
                    ((CurrentIOProvider != null && CurrentIOProvider.DirectoryExists(GetVirtualPath(path)))
                        || DirectoryExists(GetPathParts(path))
                    );
        }

        void IIOProvider.CreateDirectory(string path)
        {
            var fixedPath = FixPath(path);
            if (BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Remove(fixedPath);
            }

            if (!(this as IIOProvider).DirectoryExists(fixedPath))
            {
                CurrentIOProvider?.CreateDirectory(GetVirtualPath(fixedPath));
            }
        }

        string[] IIOProvider.GetFiles(string path, string searchPattern, bool topDirectoryOnly)
        {
            var searchPatternRegex = new Regex(GetFileSearchRegex(searchPattern), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var parts = GetPathParts(path);
            var output = new List<string>();

            void addRomFsFiles(int partitionId)
            {
                var directory = "/" + GetRomFsDirectoryName(partitionId) + "/";

                var currentDirectory = GetPartitionOrDefault(partitionId)?.RomFs?.Level3.RootDirectoryMetadataTable;
                for (int i = 1; i < parts.Length; i += 1)
                {
                    currentDirectory = currentDirectory?.ChildDirectories.Where(x => string.Compare(x.Name, parts[i], true) == 0).FirstOrDefault();
                    directory += currentDirectory.Name + "/";
                }

                if (currentDirectory != null)
                {
                    IEnumerable<string> files;
                    if (ReferenceEquals(currentDirectory, GetPartitionOrDefault(partitionId).RomFs.Level3.RootDirectoryMetadataTable))
                    {
                        // The root RomFS directory doesn't contain files; those are located in the level 3
                        files = GetPartitionOrDefault(partitionId).RomFs.Level3.RootFiles
                        .Where(f => searchPatternRegex.IsMatch(f.Name))
                        .Select(f => directory + f.Name);
                    }
                    else
                    {
                        files = currentDirectory.ChildFiles
                        .Where(f => searchPatternRegex.IsMatch(f.Name))
                        .Select(f => directory + f.Name);
                    }

                    output.AddRange(files);

                    if (!topDirectoryOnly)
                    {
                        foreach (var d in currentDirectory.ChildDirectories)
                        {
                            output.AddRange((this as IIOProvider).GetFiles(directory + d.Name + "/", searchPattern, topDirectoryOnly));
                        }
                    }
                }
            }

            var dirName = parts[0].ToLower();
            switch (dirName)
            {
                case "" when parts.Length == 1:
                    if (!topDirectoryOnly)
                    {
                        for (int i = 0; i < Partitions.Length; i++)
                        {
                            if (Partitions[i].ExHeader != null)
                            {
                                var exheaderName = GetExHeaderFileName(i);
                                if (!searchPatternRegex.IsMatch(exheaderName))
                                {
                                    output.Add(exheaderName);
                                }
                            }
                            if (Partitions[i].ExeFs != null)
                            {
                                output.AddRange((this as IIOProvider).GetFiles("/" + GetExeFsDirectoryName(i), searchPattern, topDirectoryOnly));
                            }
                            if (Partitions[i].RomFs != null)
                            {
                                output.AddRange((this as IIOProvider).GetFiles("/" + GetRomFsDirectoryName(i), searchPattern, topDirectoryOnly));
                            }
                        }
                    }
                    break;
                case "exefs" when parts.Length == 1:
                case "exefs-partition-0" when parts.Length == 1:
                    foreach (var file in GetPartitionOrDefault(0)?.ExeFs?.Headers
                        ?.Where(h => searchPatternRegex.IsMatch(h.Filename) && !string.IsNullOrWhiteSpace(h.Filename))
                        ?.Select(h => h.Filename))
                    {
                        output.Add("/ExeFS/" + file);
                    }
                    break;
                case "romfs":
                case "romfs-partition-0":
                    addRomFsFiles(0);
                    break;
                case "manual":
                case "romfs-partition-1":
                    addRomFsFiles(1);
                    break;
                case "downloadplay":
                case "romfs-partition-2":
                    addRomFsFiles(2);
                    break;
                case "n3dsupdate":
                case "romfs-partition-6":
                    addRomFsFiles(6);
                    break;
                case "o3dsupdate":
                case "romfs-partition-7":
                    addRomFsFiles(7);
                    break;
                default:
                    if (dirName.StartsWith("romfs-partition-"))
                    {
                        var partitionNumRaw = dirName.Split("-".ToCharArray(), 3)[2];
                        if (int.TryParse(partitionNumRaw, out var partitionNum))
                        {
                            addRomFsFiles(partitionNum);
                        }
                    }
                    else if (dirName.StartsWith("exefs-partition-"))
                    {
                        var partitionNumRaw = dirName.Split("-".ToCharArray(), 3)[2];
                        if (int.TryParse(partitionNumRaw, out var partitionNum))
                        {
                            foreach (var file in GetPartitionOrDefault(partitionNum)?.ExeFs?.Headers
                             ?.Where(h => searchPatternRegex.IsMatch(h.Filename) && !string.IsNullOrWhiteSpace(h.Filename))
                             ?.Select(h => h.Filename))
                            {
                                output.Add("/ExeFS/" + file);
                            }
                        }
                    }
                    break;
            }

            // Apply shadowed files
            var virtualPath = GetVirtualPath(path);
            if (CurrentIOProvider != null && CurrentIOProvider.DirectoryExists(virtualPath))
            {
                foreach (var item in CurrentIOProvider.GetFiles(virtualPath, searchPattern, topDirectoryOnly))
                {
                    var overlayPath = "/" + FileSystem.MakeRelativePath(item, VirtualPath);
                    if (!BlacklistedPaths.Contains(overlayPath) && !output.Contains(overlayPath, StringComparer.OrdinalIgnoreCase))
                    {
                        output.Add(overlayPath);
                    }
                }
            }

            return output.ToArray();
        }

        string[] IIOProvider.GetDirectories(string path, bool topDirectoryOnly)
        {
            var parts = GetPathParts(path);
            var output = new List<string>();

            void addRomFsDirectories(int partitionId)
            {
                var directory = "/" + GetRomFsDirectoryName(partitionId) + "/";

                var currentDirectory = GetPartitionOrDefault(partitionId)?.RomFs?.Level3.RootDirectoryMetadataTable;
                for (int i = 1; i < parts.Length; i += 1)
                {
                    currentDirectory = currentDirectory?.ChildDirectories.Where(x => string.Compare(x.Name, parts[i], true) == 0).FirstOrDefault();
                    directory += currentDirectory.Name + "/";
                }
                if (currentDirectory != null)
                {
                    var dirs = currentDirectory.ChildDirectories
                        .Select(f => directory + f.Name + "/");
                    output.AddRange(dirs);

                    if (!topDirectoryOnly)
                    {
                        foreach (var d in currentDirectory.ChildDirectories)
                        {
                            output.AddRange((this as IIOProvider).GetDirectories(directory + d.Name + "/", topDirectoryOnly));
                        }
                    }
                }
            }

            var dirName = parts[0].ToLower();
            switch (dirName)
            {
                case "" when parts.Length == 1:
                    for (int i = 0; i < Partitions.Length; i++)
                    {
                        if (Partitions[i].ExeFs != null)
                        {
                            output.Add("/" + GetExeFsDirectoryName(i) + "/");
                            if (!topDirectoryOnly)
                            {
                                output.AddRange((this as IIOProvider).GetDirectories("/" + GetExeFsDirectoryName(i), topDirectoryOnly));
                            }
                        }
                        if (Partitions[i].RomFs != null)
                        {
                            output.Add("/" + GetRomFsDirectoryName(i)  + "/");
                            if (!topDirectoryOnly)
                            {
                                output.AddRange((this as IIOProvider).GetDirectories("/" + GetRomFsDirectoryName(i), topDirectoryOnly));
                            }
                        }
                    }
                    break;
                case "exefs" when parts.Length == 1:
                    // ExeFs doesn't support directories
                    break;
                case "romfs":
                case "romfs-partition-0":
                    addRomFsDirectories(0);
                    break;
                case "manual":
                case "romfs-partition-1":
                    addRomFsDirectories(1);
                    break;
                case "downloadplay":
                case "romfs-partition-2":
                    addRomFsDirectories(2);
                    break;
                case "n3dsupdate":
                case "romfs-partition-6":
                    addRomFsDirectories(6);
                    break;
                case "o3dsupdate":
                case "romfs-partition-7":
                    addRomFsDirectories(7);
                    break;
                default:
                    if (dirName.StartsWith("romfs-partition-"))
                    {
                        var partitionNumRaw = dirName.Split("-".ToCharArray(), 3)[2];
                        if (int.TryParse(partitionNumRaw, out var partitionNum))
                        {
                            addRomFsDirectories(partitionNum);
                        }
                    }
                    break;
            }

            // Apply shadowed files
            var virtualPath = GetVirtualPath(path);
            if (CurrentIOProvider != null && CurrentIOProvider.DirectoryExists(virtualPath))
            {
                foreach (var item in CurrentIOProvider.GetDirectories(virtualPath, topDirectoryOnly))
                {
                    var overlayPath = "/" + FileSystem.MakeRelativePath(item, VirtualPath);
                    if (!BlacklistedPaths.Contains(overlayPath) && !output.Contains(overlayPath, StringComparer.OrdinalIgnoreCase))
                    {
                        output.Add(overlayPath);
                    }
                }
            }

            return output.ToArray();
        }

        byte[] IIOProvider.ReadAllBytes(string filename)
        {
            var fixedPath = FixPath(filename);
            if (BlacklistedPaths.Contains(fixedPath))
            {
                throw new FileNotFoundException(string.Format(Properties.Resources.ThreeDsRom_ErrorRomFileNotFound, filename), filename);
            }
            else
            {
                var virtualPath = GetVirtualPath(fixedPath);
                if (CurrentIOProvider != null && CurrentIOProvider.FileExists(virtualPath))
                {
                    return CurrentIOProvider.ReadAllBytes(virtualPath);
                }
                else
                {
                    var data = GetDataReference(GetPathParts(filename));
                    return data.Read();
                }
            }
        }

        string IIOProvider.ReadAllText(string filename)
        {
            return Encoding.UTF8.GetString((this as IIOProvider).ReadAllBytes(filename));
        }

        void IIOProvider.WriteAllBytes(string filename, byte[] data)
        {
            var fixedPath = FixPath(filename);
            if (BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Remove(fixedPath);
            }

            CurrentIOProvider?.WriteAllBytes(GetVirtualPath(filename), data);
        }

        void IIOProvider.WriteAllText(string filename, string data)
        {
            (this as IIOProvider).WriteAllBytes(filename, Encoding.UTF8.GetBytes(data));
        }

        void IIOProvider.CopyFile(string sourceFilename, string destinationFilename)
        {
            (this as IIOProvider).WriteAllBytes(destinationFilename, (this as IIOProvider).ReadAllBytes(sourceFilename));
        }

        void IIOProvider.DeleteFile(string filename)
        {
            var fixedPath = FixPath(filename);
            if (!BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Add(fixedPath);
            }

            var virtualPath = GetVirtualPath(filename);
            if (CurrentIOProvider != null && CurrentIOProvider.FileExists(virtualPath))
            {
                CurrentIOProvider.DeleteFile(virtualPath);
            }
        }

        void IIOProvider.DeleteDirectory(string path)
        {
            var fixedPath = FixPath(path);
            if (!BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Add(fixedPath);
            }

            var virtualPath = GetVirtualPath(path);
            if (CurrentIOProvider != null && CurrentIOProvider.FileExists(virtualPath))
            {
                CurrentIOProvider.DeleteFile(virtualPath);
            }
        }

        string IIOProvider.GetTempFilename()
        {
            var path = "/temp/files/" + Guid.NewGuid().ToString();
            (this as IIOProvider).WriteAllBytes(path, new byte[] { });
            return path;
        }

        string IIOProvider.GetTempDirectory()
        {
            var path = "/temp/dirs/" + Guid.NewGuid().ToString();
            (this as IIOProvider).CreateDirectory(path);
            return path;
        }

        Stream IIOProvider.OpenFile(string filename)
        {
            if (CurrentIOProvider != null)
            {
                throw new NotSupportedException("Cannot open a file as a stream without an IO provider.");
            }

            var virtualPath = GetVirtualPath(filename);
            if (!CurrentIOProvider.DirectoryExists(virtualPath))
            {
                CurrentIOProvider.CreateDirectory(virtualPath);
            }
            CurrentIOProvider.WriteAllBytes(virtualPath, (this as IIOProvider).ReadAllBytes(filename));

            return CurrentIOProvider.OpenFile(filename);
        }

        Stream IIOProvider.OpenFileReadOnly(string filename)
        {
            if (CurrentIOProvider != null)
            {
                throw new NotSupportedException("Cannot open a file as a stream without an IO provider.");
            }

            var virtualPath = GetVirtualPath(filename);
            if (!CurrentIOProvider.DirectoryExists(virtualPath))
            {
                CurrentIOProvider.CreateDirectory(virtualPath);
            }
            CurrentIOProvider.WriteAllBytes(virtualPath, (this as IIOProvider).ReadAllBytes(filename));

            return CurrentIOProvider.OpenFileReadOnly(filename);
        }

        Stream IIOProvider.OpenFileWriteOnly(string filename)
        {
            if (CurrentIOProvider != null)
            {
                throw new NotSupportedException("Cannot open a file as a stream without an IO provider.");
            }

            var virtualPath = GetVirtualPath(filename);
            if (!CurrentIOProvider.DirectoryExists(virtualPath))
            {
                CurrentIOProvider.CreateDirectory(virtualPath);
            }
            CurrentIOProvider.WriteAllBytes(virtualPath, (this as IIOProvider).ReadAllBytes(filename));

            return CurrentIOProvider.OpenFileWriteOnly(filename);
        }

        #endregion

        #region IDetectableFileType Implementation

        public async Task<bool> IsOfType(GenericFile file)
        {
            return await NcsdFile.IsNcsd(file) || await CiaFile.IsCia(file) || await NcchPartition.IsNcch(file) || await RomFs.IsRomFs(file) || await ExeFs.IsExeFs(file);
        }

        #endregion
    }
}
