using GorillaNetworking;
using Jerald;
using System.Text;

namespace GorillaHands.Pages;

[AutoRegister]
public class ConfigPage : Page
{
    public override string PageName => "Hands";

    private string content;

    public ConfigPage()
    {
        Configuration.Config.SettingChanged += (sender, obj) => BuildPage();

        OnKeyPressed += (key) =>
        {
            if (key.Binding == GorillaKeyboardBindings.option1)
            {
                Configuration.ArmOffsetMultiplier.Value = 0;
                return;
            }
            if (int.TryParse(key.characterString, out int value)) Configuration.ArmOffsetMultiplier.Value += value;
            UpdateContent();
        };
        BuildPage();
    }

    private void BuildPage()
    {
        var builder = new StringBuilder("Gorilla Hands v1.0.0")
            .AppendLine("Adjust config")
            .AppendLine($"Arm Offset: [{Configuration.ArmOffsetMultiplier.Value}]   |   Option1 = reset");
        content = builder.ToString();
    }

    public override string GetContent() => content;
}