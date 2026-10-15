using Scout.Web.Features.InterviewWorkspace;
namespace Scout.Web.Tests.InterviewWorkspace;

internal static class ReviewedApproval
{
    internal static async Task ReviewedApproveAsync(this WorkspaceOperations operations, int id, WorkspaceApproval input)
    {
        input.ExpectedReviewStamp = (await operations.SetupAsync(id))!.ReviewStamp;
        await operations.ApproveAsync(id, input);
    }
}
