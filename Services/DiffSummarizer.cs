using AICodeReviewer.Api.Models;
using AICodeReviewer.Api.Models;

namespace AICodeReviewer.Api.Services;

public class DiffSummarizer
{
    public (int files, int adds, int dels) Tally(IEnumerable<FileChange> files)
    {
        int f = 0, a = 0, d = 0;
        foreach (var x in files)
        {
            f++;
            a += x.Additions;
            d += x.Deletions;
        }
        return (f, a, d);
    }
}
