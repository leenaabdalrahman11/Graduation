using MyApi.DAL.Models;

namespace MyApi.BLL.Service;

public class VoiceContextResolver : IVoiceContextResolver
{
    public CommandInterpretation? Resolve(
        CommandInterpretation? interpretation,
        VoiceSearchSessionState state,
        string userText,
        string? language)
    {
        if (interpretation == null)
            return interpretation;

        if (state.LastAction == "ViewOrders" &&
            interpretation.Action == "OpenOrderDetails")
        {
            return interpretation;
        }

        if (!string.IsNullOrWhiteSpace(interpretation.ConversationMode) &&
            interpretation.Action != "Clarify")
        {
            return interpretation;
        }

        if (state.AwaitingFinalSearchConfirmation &&
            interpretation.Action == "SearchProduct" &&
            interpretation.ShouldSearchNow)
        {
            return interpretation;
        }

        if (state.AwaitingPaymentConfirmation &&
            interpretation.Action == "OpenPayment")
        {
            return interpretation;
        }

        if (state.LastShownProducts != null &&
            state.LastShownProducts.Count > 0 &&
            (
                interpretation.Action == "AddToCart" ||
                interpretation.Action == "OpenProductDetails"
            ))
        {
            return interpretation;
        }

        return interpretation;
    }
}