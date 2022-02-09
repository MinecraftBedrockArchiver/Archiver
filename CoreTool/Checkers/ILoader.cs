using CoreTool.Archive;
using System.Threading.Tasks;

namespace CoreTool.Checkers
{
    internal interface IChecker
    {
        Task Check(ArchiveMeta archive);
    }
}
