using CoreTool.Archive;
using System.Threading.Tasks;

namespace CoreTool.Loaders
{
    internal interface ILoader
    {
        Task Load(ArchiveMeta archive);
    }
}
