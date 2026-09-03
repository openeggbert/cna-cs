// Same XNA source, three engines, timestep uncapped. Reports frames per second for a fixed
// workload, so the number is the framework's own cost rather than a 60 Hz target being met.
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EngineBench
{
    public class BenchGame : Game
    {
        readonly string mode;
        readonly int frames;
        GraphicsDeviceManager graphics;
        SpriteBatch batch;
        Texture2D pixel;
        BasicEffect effect;
        VertexPositionColor[] tris;
        Stopwatch clock = new Stopwatch();
        long allocAtStart;
        int gen0AtStart;
        Stopwatch drawTimer = new Stopwatch();
        Stopwatch endTimer = new Stopwatch();
        int drawn;

        public BenchGame(string mode, int frames)
        {
            this.mode = mode;
            this.frames = frames;
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
            graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = false;
        }

        protected override void LoadContent()
        {
            batch = new SpriteBatch(GraphicsDevice);
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new Color[] { Color.White });

            effect = new BasicEffect(GraphicsDevice);
            effect.VertexColorEnabled = true;
            effect.Projection = Matrix.CreateOrthographicOffCenter(0, 800, 600, 0, 0, 1);

            var rng = new Random(1234);
            tris = new VertexPositionColor[3 * 2000];
            for (int i = 0; i < tris.Length; i += 3)
            {
                float x = rng.Next(0, 780), y = rng.Next(0, 580);
                var c = new Color(rng.Next(64, 255), rng.Next(64, 255), rng.Next(64, 255));
                tris[i] = new VertexPositionColor(new Vector3(x, y, 0), c);
                tris[i + 1] = new VertexPositionColor(new Vector3(x + 16, y, 0), c);
                tris[i + 2] = new VertexPositionColor(new Vector3(x, y + 16, 0), c);
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            // Frames 1..30 are warm-up: shader compilation, buffer creation and JIT all land there.
            if (drawn == 30) { clock.Start(); allocAtStart = GC.GetAllocatedBytesForCurrentThread(); gen0AtStart = GC.CollectionCount(0); }

            GraphicsDevice.Clear(Color.Black);

            if (mode == "sprites")
            {
                batch.Begin();
                for (int i = 0; i < 2000; i++)
                    batch.Draw(pixel, new Rectangle((i * 37) % 780, (i * 53) % 580, 16, 16), Color.White);
                batch.End();
            }
            else if (mode == "spritesplit")
            {
                // Same workload as "sprites", but timing the buffering loop apart from the flush,
                // so the framework's per-sprite CPU cost is separated from whatever End() hands to
                // the renderer. Only accumulates once warm-up is over.
                bool measuring = drawn >= 30;
                if (measuring) drawTimer.Start();
                batch.Begin();
                for (int i = 0; i < 2000; i++)
                    batch.Draw(pixel, new Rectangle((i * 37) % 780, (i * 53) % 580, 16, 16), Color.White);
                if (measuring) { drawTimer.Stop(); endTimer.Start(); }
                batch.End();
                if (measuring) endTimer.Stop();
            }
            else if (mode == "tris")
            {
                effect.CurrentTechnique.Passes[0].Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, tris, 0, 2000);
            }
            // mode "loop" draws only the clear.

            drawn++;
            if (drawn >= frames + 30)
            {
                clock.Stop();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - allocAtStart;
                int gen0 = GC.CollectionCount(0) - gen0AtStart;
                double fps = (frames) / clock.Elapsed.TotalSeconds;
                Console.WriteLine($"ALLOC bytesPerFrame={allocated / (double)frames:F0} gen0Collections={gen0}");
                Console.WriteLine($"RESULT mode={mode} frames={frames} seconds={clock.Elapsed.TotalSeconds:F3} fps={fps:F1}");
                if (mode == "spritesplit")
                {
                    Console.WriteLine($"SPLIT drawUsPerFrame={drawTimer.Elapsed.TotalMilliseconds * 1000.0 / frames:F1} endUsPerFrame={endTimer.Elapsed.TotalMilliseconds * 1000.0 / frames:F1}");
                }
                Exit();
            }
            base.Draw(gameTime);
        }

        static void Main(string[] args)
        {
            string mode = args.Length > 0 ? args[0] : "loop";
            int frames = args.Length > 1 ? int.Parse(args[1]) : 300;
            using (var g = new BenchGame(mode, frames)) g.Run();
        }
    }
}
