var gamePath = Argument<string>("gamepath", "/home/crafterbot/.local/share/Steam/steamapps/common/Gorilla Tag/");

Task("Build")
    .Does(() =>
{
    Information("Building project...");
    EnvironmentVariable("GORILLATAG_PATH", gamePath);
    DotNetBuild("./GorillaHands.csproj");
});

RunTarget("Build");
