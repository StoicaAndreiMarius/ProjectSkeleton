using Silk.NET.SDL;

namespace TheAdventure;

public static class Program
{
    public static async Task Main()
    {
        var sdl = new Sdl(new SdlContext());

        var init = sdl.Init(Sdl.InitVideo | Sdl.InitEvents | Sdl.InitTimer);
        if (init < 0)
        {
            throw new InvalidOperationException("Failed to initialize SDL.");
        }

        using (var window = new GameWindow(sdl))
        {
            var input = new Input(sdl);
            var renderer = new GameRenderer(sdl, window);
            var engine = new Engine(renderer, input);

            await engine.SetupWorldAsync();

            bool quit = false;
            while (!quit)
            {
                quit = input.ProcessInput();
                if (quit) break;

                engine.ProcessFrame();
                engine.RenderFrame();
                System.Threading.Thread.Sleep(13);
            }

            await engine.PersistHighScoreAsync();
        }

        sdl.Quit();
    }
}
