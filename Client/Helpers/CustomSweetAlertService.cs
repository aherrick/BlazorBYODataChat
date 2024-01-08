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
}