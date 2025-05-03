namespace Binnaculum.Extensions;

public class RxImageEvents(Image data) : RxVisualElementEvents(data)
{
    private readonly Image _data = data;
}
