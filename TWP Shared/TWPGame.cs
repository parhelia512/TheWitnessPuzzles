using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;
using System.Linq;
using TWP_Shared;
using TheWitnessPuzzles;
using System.Threading;

namespace TWP_Shared
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class TWPGame : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Point defaultScreenSize = new Point(400, 720);
        bool isMobile;

        public TWPGame(bool isMobile)
        {
            this.isMobile = isMobile;
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            InputManager.Initialize(this);
            SettingsManager.LoadSettings();

#if WINDOWS
            // This method is called twice (here and in the Initialize method later), because of cross-platform magic
            // On Windows GraphicsDevice should me initialized in constructor, otherwise window size will be default and not corresponding to the backbuffer
            // But on Android GraphicsDevice is still null after ApplyChanges() if it's called in constructor
            InitializeGraphicsDevice();
#endif

            Window.ClientSizeChanged += ResizeScreen;
        }

        private void ResizeScreen(object sender, EventArgs e)
        {
            ScreenManager.Instance.UpdateScreenSize(GraphicsDevice.Viewport.Bounds.Size);
        }

        public void SetFullscreen(bool isFullscreen)
        {
#if ANDROID
            graphics.IsFullScreen = isFullscreen;
#else
            if (isFullscreen)
            {
                Window.IsBorderless = true;
                graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
            else
            {
                Window.IsBorderless = false;
                graphics.PreferredBackBufferWidth = defaultScreenSize.X;
                graphics.PreferredBackBufferHeight = defaultScreenSize.Y;
            }
#endif
            graphics.ApplyChanges();
            ResizeScreen(null, null);
        }

        // To be removed later
        private Puzzle CreateTestPanel()
        {
            Puzzle panel = new SymmetryPuzzle(5, 5, true, Color.Aqua, Color.Yellow);
            panel.Nodes[25].SetState(NodeState.Start);
            panel.Nodes[28].SetState(NodeState.Start);
            panel.Nodes[7].SetState(NodeState.Start);
            panel.Nodes[10].SetState(NodeState.Start);
            panel.Nodes[1].SetState(NodeState.Exit);
            panel.Nodes[3].SetState(NodeState.Exit);
            panel.Nodes[23].SetState(NodeState.Exit);
            panel.Nodes[22].SetState(NodeState.Exit);
            panel.Nodes[12].SetState(NodeState.Exit);
            panel.Nodes[0].SetState(NodeState.Exit);
            panel.Nodes[20].SetState(NodeState.Exit);
            panel.Nodes[9].SetState(NodeState.Exit);
            panel.Nodes[24].SetState(NodeState.Exit);
            panel.Nodes[32].SetState(NodeState.Exit);
            panel.Edges.Find(x => x.Id == 814)?.SetState(EdgeState.Broken);
            panel.Edges.Find(x => x.Id == 1617)?.SetState(EdgeState.Broken);

            panel.Nodes[14].SetStateAndColor(NodeState.Marked, Color.Yellow);
            panel.Nodes[18].SetState(NodeState.Marked);
            panel.Edges.Find(x => x.Id == 2021)?.SetStateAndColor(EdgeState.Marked, Color.Aqua);

            panel.Grid[2, 0].Rule = new ColoredSquareRule(Color.Magenta);
            panel.Grid[1, 1].Rule = new SunPairRule(Color.Magenta);
            panel.Grid[4, 4].Rule = new ColoredSquareRule(Color.Lime);
            panel.Grid[1, 4].Rule = new SunPairRule(Color.Lime);
            panel.Grid[2, 1].Rule = new TriangleRule(1);
            panel.Grid[3, 2].Rule = new TriangleRule(2);
            panel.Grid[3, 1].Rule = new TriangleRule(3);
            panel.Grid[4, 2].Rule = new EliminationRule();
            panel.Grid[2, 3].Rule = new EliminationRule(Color.Magenta);

            return panel;
        }
        private Puzzle CreateTestPanel2()
        {
            Puzzle panel = new SymmetryPuzzle(7, 4, true, Color.Aqua, Color.Yellow);
            panel.Nodes[16].SetState(NodeState.Start);
            panel.Nodes[23].SetState(NodeState.Start);
            panel.Nodes[3].SetState(NodeState.Exit);
            panel.Nodes[36].SetState(NodeState.Exit);
            panel.Nodes[34].SetState(NodeState.Marked);
            panel.Edges.Find(x => x.Id == 1819)?.SetState(EdgeState.Marked);
            panel.Edges.Find(x => x.Id == 1920)?.SetState(EdgeState.Marked);
            panel.Grid[0, 2].Rule = new ColoredSquareRule(Color.Yellow);
            panel.Grid[2, 0].Rule = new ColoredSquareRule(Color.Aqua);
            panel.Grid[4, 0].Rule = new ColoredSquareRule(Color.Yellow);
            panel.Grid[6, 1].Rule = new ColoredSquareRule(Color.Yellow);
            panel.Grid[3, 3].Rule = new SunPairRule(Color.Yellow);
            panel.Grid[2, 2].Rule = new EliminationRule();
            panel.Grid[3, 2].Rule = new EliminationRule();
            panel.Grid[6, 0].Rule = new TriangleRule(1);
            panel.Grid[5, 1].Rule = new TriangleRule(2);

            panel.Grid[5, 2].Rule = new TetrisRotatableRule(new bool[,] { { false, true }, {false, true }, { true, true } });
            panel.Grid[4, 3].Rule = new TetrisRule(new bool[,] { { false, true }, { true, true }, { false, true } });
            panel.Grid[5, 0].Rule = new TetrisRule(new bool[,] { { true } }, true);

            return panel;
        }


        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
#if ANDROID
            // This method is called twice (here and in the Constructor earlier), because of cross-platform magic
            // On Windows GraphicsDevice should me initialized in constructor, otherwise window size will be default and not corresponding to the backbuffer
            // But on Android GraphicsDevice is still null after ApplyChanges() if it's called in constructor
            InitializeGraphicsDevice();
#endif

            ScreenManager.Instance.Initialize(this, GraphicsDevice, Content);
            ScreenManager.Instance.ScreenSize = new Point(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

            base.Initialize();
        }

        private void InitializeGraphicsDevice()
        {
#if ANDROID

            graphics.IsFullScreen = SettingsManager.IsFullscreen;
            graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            graphics.SupportedOrientations = DisplayOrientation.Portrait;
            TouchPanel.EnabledGestures = GestureType.Tap | GestureType.FreeDrag;
            //graphics.GraphicsProfile = GraphicsProfile.HiDef;
            graphics.ApplyChanges();

#else
            
            if (SettingsManager.IsFullscreen)
            {
                Window.IsBorderless = true;
                graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
            else
            {
                graphics.PreferredBackBufferWidth = defaultScreenSize.X;
                graphics.PreferredBackBufferHeight = defaultScreenSize.Y;
            }

            Window.AllowUserResizing = true;
            IsMouseVisible = true;
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            graphics.ApplyChanges();

#endif
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            ScreenManager.Instance.LoadContent();
            SoundManager.LoadContent(Content);

            InitializeAfterContentIsLoaded();
        }

        protected virtual void InitializeAfterContentIsLoaded()
        {
            ScreenManager.Instance.AddScreen<SplashGameScreen>();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent() { }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (InputManager.IsKeyPressed(Keys.Enter))
                ScreenManager.Instance.AddScreen<PanelGameScreen>(true, true, DI.Get<PanelGenerator>().GeneratePanel());

            ScreenManager.Instance.Update(gameTime);
            InputManager.Update();
            base.Update(gameTime);
        }
        
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();
            ScreenManager.Instance.Draw(spriteBatch);
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
