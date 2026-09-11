using MyApi.DAL.DTO.Response;
using MyApi.DAL.Models;

namespace MyApi.BLL.Service;

public class VoiceSearchSessionState
{
    public string LastAction { get; set; } = "";
    public string LastQuery { get; set; } = "";
    public string PendingIntent { get; set; } = "";
    public ProductSearchContext? SearchContext { get; set; }
    public string PendingSlot { get; set; } = "";
    public List<ProductUserResponse> LastMatchedProducts { get; set; } = new();
    public int LastShownIndex { get; set; } = 0;
    public bool SkipFinalSummaryOnce { get; set; } = false;
    public bool AwaitingFinalSearchConfirmation { get; set; } = false;
    public string LastAssistantReply { get; set; } = "";
    public bool AwaitingFilterChoice { get; set; }
    public bool SkipFilterStepOnce { get; set; }
    public bool AwaitingResultRefinement { get; set; }
    public bool ResultsReadyButNotShown { get; set; }
    public bool AwaitingPaymentConfirmation { get; set; }
    public List<ProductUserResponse> LastShownProducts { get; set; } = new();
}