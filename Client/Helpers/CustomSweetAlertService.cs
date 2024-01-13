using CurrieTechnologies.Razor.SweetAlert2;

namespace Client.Helpers;

public class CustomSweetAlertService(SweetAlertService sweetAlertService)
{
    public async Task Error(string msg)
    {
        await Fire(":(", msg, SweetAlertIcon.Error);
    }

    public async Task Success(string msg)
    {
        await Fire(nameof(Success), msg, SweetAlertIcon.Success);
    }

    public async Task Fire(string title, string msg, SweetAlertIcon icon)
    {
        await sweetAlertService.FireAsync(
              title,
              msg,
              icon
          );
    }

    public async Task Confirm(string title, string msg, Action yesAction, Action noAction = null)
    {
        var result = await sweetAlertService.FireAsync(new SweetAlertOptions
        {
            Title = title,
            Text = msg,
            Icon = SweetAlertIcon.Warning,
            ShowCancelButton = true,
            ConfirmButtonText = "Yes",
            CancelButtonText = "Cancel"
        });

        if (!string.IsNullOrEmpty(result.Value))
        {
            yesAction();
        }
        else if (result.Dismiss == DismissReason.Cancel && noAction != null)
        {
            noAction();
        }
    }
}