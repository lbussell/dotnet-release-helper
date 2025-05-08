
using ConsoleAppFramework;

var app = ConsoleApp.Create();
app.Add<Commands>();
app.Run(args);

class Commands
{
    public void Commits(string to)
    {
        Console.WriteLine("List of commits to {0}", to);
    }
}
