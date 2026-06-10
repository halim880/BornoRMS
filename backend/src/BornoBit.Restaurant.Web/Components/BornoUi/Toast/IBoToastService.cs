namespace BornoBit.Restaurant.Web.Components.BornoUi.Toast;

public interface IBoToastService
{
    void Show(BoToastKind kind, string message, string? title = null, int? durationMs = null);
    void ShowSuccess(string message, string? title = null, int? durationMs = null);
    void ShowError(string message, string? title = null, int? durationMs = null);
    void ShowWarning(string message, string? title = null, int? durationMs = null);
    void ShowInfo(string message, string? title = null, int? durationMs = null);
    void Dismiss(Guid id);
    void Clear();
}
