using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameProject
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // game objects. Using inheritance would make this
        // easier, but inheritance isn't a GDD 1200 topic
        Burger burger;
        List<TeddyBear> bears = new List<TeddyBear>();
        static List<Projectile> projectiles = new List<Projectile>();
        List<Explosion> explosions = new List<Explosion>();

        // projectile and explosion sprites. Saved so they don't have to
        // be loaded every time projectiles or explosions are created
        static Texture2D frenchFriesSprite;
        static Texture2D teddyBearProjectileSprite;
        static Texture2D explosionSpriteStrip;

        // scoring support
        int score = 0;
        string scoreString = GameConstants.ScorePrefix + 0;

        // health support
        string healthString = GameConstants.HealthPrefix +
            GameConstants.BurgerInitialHealth;
        bool burgerDead = false;

        // text display support
        SpriteFont font;

        // sound effects
        SoundEffect burgerDamage;
        SoundEffect burgerDeath;
        SoundEffect burgerShot;
        SoundEffect explosionSound;
        SoundEffect teddyBounce;
        SoundEffect teddyShot;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // set resolution
            graphics.PreferredBackBufferWidth = GameConstants.WindowWidth;
            graphics.PreferredBackBufferHeight = GameConstants.WindowHeight;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            RandomNumberGenerator.Initialize();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // load audio content
            burgerDamage = Content.Load<SoundEffect>(@"audio\BurgerDamage");
            burgerDeath = Content.Load<SoundEffect>(@"audio\BurgerDeath");
            burgerShot = Content.Load<SoundEffect>(@"audio\BurgerShot");
            explosionSound = Content.Load<SoundEffect>(@"audio\Explosion");
            teddyBounce = Content.Load<SoundEffect>(@"audio\TeddyBounce");
            teddyShot = Content.Load<SoundEffect>(@"audio\TeddyShot");

            // load sprite font
            font = Content.Load<SpriteFont>(@"fonts\Arial20");

            // load projectile and explosion sprites
            teddyBearProjectileSprite = Content.Load<Texture2D>(@"graphics\teddybearprojectile");
            frenchFriesSprite = Content.Load<Texture2D>(@"graphics\frenchfries");
            explosionSpriteStrip = Content.Load<Texture2D>(@"graphics\explosion");


            // add initial game objects
            burger = new GameProject.Burger(Content, @"graphics\burger", GameConstants.WindowWidth / 2, (GameConstants.WindowHeight / 8) * 7, burgerShot);

            for (int i = 0; i < GameConstants.MaxBears; i++)
            {
                SpawnBear(); 
            }

            // set initial health and score strings
            healthString = GameConstants.HealthPrefix + burger.Health;
            scoreString = GameConstants.ScorePrefix;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // get current mouse state and update burger
            MouseState mouse = Mouse.GetState();

            // get keyboard state
            KeyboardState keyboard = Keyboard.GetState();

            burger.Update(gameTime, keyboard);

            // update other game objects
            foreach (TeddyBear bear in bears)
            {
                bear.Update(gameTime);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Update(gameTime);
            }
            foreach (Explosion explosion in explosions)
            {
                explosion.Update(gameTime);
            }

            // check and resolve collisions between teddy bears
            for (int i = 0; i < bears.Count; i++)
            {
                for (int j = i + 1; j < bears.Count; j++)
                {
                    if (bears[i].Active && bears[j].Active)
                    {
                        CollisionResolutionInfo collision = CollisionUtils.CheckCollision(gameTime.ElapsedGameTime.Milliseconds, GameConstants.WindowWidth, GameConstants.WindowHeight,
                                                                                    bears[i].Velocity, bears[i].DrawRectangle, bears[j].Velocity, bears[j].DrawRectangle);

                        if (collision != null)
                        {
                            teddyBounce.Play();
                            if (collision.FirstOutOfBounds)
                            {
                                bears[i].Active = false;
                            }
                            else
                            {
                                bears[i].Velocity = collision.FirstVelocity;
                                bears[i].DrawRectangle = collision.FirstDrawRectangle;
                            }

                            if (collision.SecondOutOfBounds)
                            {
                                bears[j].Active = false;
                            }
                            else
                            {
                                bears[j].Velocity = collision.SecondVelocity;
                                bears[j].DrawRectangle = collision.SecondDrawRectangle;
                            }
                            

                        }
                    }
                }
            }


            // check and resolve collisions between burger and teddy bears
            foreach (var bear in bears)
            {
                if (bear.Active && burger.CollisionRectangle.Intersects(bear.CollisionRectangle))
                {
                    bear.Active = false;
                    burger.Health -= GameConstants.BearDamage;
                    CheckBurgerKill();
                    healthString = GameConstants.HealthPrefix + burger.Health;
                    // add sound
                    burgerDamage.Play(0.8f,0,0);

                    Explosion explosion = new GameProject.Explosion(explosionSpriteStrip, bear.Location.X, bear.Location.Y, explosionSound);
                    explosions.Add(explosion);
                }
            }

            // check and resolve collisions between burger and projectiles
            foreach (var projectile in projectiles)
            {
                if (projectile.Type == ProjectileType.TeddyBear)
                {
                    if (projectile.Active && projectile.CollisionRectangle.Intersects(burger.CollisionRectangle))
                    {
                        projectile.Active = false;
                        burger.Health -= GameConstants.TeddyBearProjectileDamage;
                        CheckBurgerKill();
                        // add sound
                        burgerDamage.Play(0.8f,0,0);
                    }
                }
            }


            // check and resolve collisions between teddy bears and projectiles
            foreach (var bear in bears)
            {
                foreach (var projectile in projectiles)
                {
                    if (projectile.Type == ProjectileType.FrenchFries)
                    {
                        if (projectile.Active && bear.Active && projectile.CollisionRectangle.Intersects(bear.CollisionRectangle))
                        {
                            projectile.Active = false;
                            bear.Active = false;
                            score += GameConstants.BearPoints;
                            scoreString = GameConstants.ScorePrefix + score;

                            Explosion explosion = new GameProject.Explosion(explosionSpriteStrip, bear.Location.X, bear.Location.Y, explosionSound);
                            explosions.Add(explosion);
                        }
                    }
                }
            }

            // clean out inactive teddy bears and add new ones as necessary
            for (int i = bears.Count - 1; i >= 0 ; i--)
            {
                if (!bears[i].Active)
                {
                    bears.RemoveAt(i);
                }
            }

            // add new teddy bears
            while (bears.Count < GameConstants.MaxBears)
            {
                SpawnBear();
            }


            // clean out inactive projectiles
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                if (!projectiles[i].Active)
                {
                    projectiles.RemoveAt(i);
                }
            }

            // clean out finished explosions
            for (int i = explosions.Count - 1; i >= 0; i--)
            {
                if (explosions[i].Finished)
                {
                    explosions.RemoveAt(i);
                }
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            // draw game objects
            burger.Draw(spriteBatch);
            foreach (TeddyBear bear in bears)
            {
                bear.Draw(spriteBatch);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Draw(spriteBatch);
            }
            foreach (Explosion explosion in explosions)
            {
                explosion.Draw(spriteBatch);
            }

            // draw score and health
            spriteBatch.DrawString(font, healthString, GameConstants.HealthLocation, Color.White);
            spriteBatch.DrawString(font, scoreString, GameConstants.ScoreLocation, Color.White);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        #region Public methods

        /// <summary>
        /// Gets the projectile sprite for the given projectile type
        /// </summary>
        /// <param name="type">the projectile type</param>
        /// <returns>the projectile sprite for the type</returns>
        public static Texture2D GetProjectileSprite(ProjectileType type)
        {
            // replace with code to return correct projectile sprite based on projectile type
            //return frenchFriesSprite;
            if (type == ProjectileType.FrenchFries)
            {
                return frenchFriesSprite;
            }
            else
            {
                return teddyBearProjectileSprite;
            }

        }

        /// <summary>
        /// Adds the given projectile to the game
        /// </summary>
        /// <param name="projectile">the projectile to add</param>
        public static void AddProjectile(Projectile projectile)
        {
            projectiles.Add(projectile);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Spawns a new teddy bear at a random location
        /// </summary>
        private void SpawnBear()
        {
            // generate random location
            int locationX = GetRandomLocation(GameConstants.SpawnBorderSize, GameConstants.WindowWidth);
            int locationY = GetRandomLocation(GameConstants.SpawnBorderSize, GameConstants.WindowHeight);

            // generate random velocity
            float speed1 = RandomNumberGenerator.NextFloat(GameConstants.BearSpeedRange);

            float speed2 = RandomNumberGenerator.NextFloat(GameConstants.BearSpeedRange);

            /*     -------  Vidi za angle generator --------------
             RandomNumberGenerator.NextFloat(GameConstants.BearSpeedRange);
            // generate random angle            float angle = (float)Math.PI * RandomNumberGenerator.NextFloat(360.0f) / 180.0f;
            // generate vector velocity            Vector2 velocity = new Vector2((float)Math.Cos(angle) * speed, -(float)Math.Sin(angle) * speed);
             */

            Vector2 vector = new Vector2(speed1, speed2);
            // create new bear
            TeddyBear newBear = new TeddyBear(Content, @"graphics/teddybear", locationX, locationY, vector, teddyBounce, teddyShot);

            // make sure we don't spawn into a collision
            List<Rectangle> collisionsList = GetCollisionRectangles();
            while (!CollisionUtils.IsCollisionFree(newBear.DrawRectangle, collisionsList))
            {
                newBear.X = GetRandomLocation(GameConstants.SpawnBorderSize, GameConstants.WindowWidth);
                newBear.Y = GetRandomLocation(GameConstants.SpawnBorderSize, GameConstants.WindowHeight);
            }

            // add new bear to list
            bears.Add(newBear);
        }

        /// <summary>
        /// Gets a random location using the given min and range
        /// </summary>
        /// <param name="min">the minimum</param>
        /// <param name="range">the range</param>
        /// <returns>the random location</returns>
        private int GetRandomLocation(int min, int range)
        {
            return min + RandomNumberGenerator.Next(range);
        }

        /// <summary>
        /// Gets a list of collision rectangles for all the objects in the game world
        /// </summary>
        /// <returns>the list of collision rectangles</returns>
        private List<Rectangle> GetCollisionRectangles()
        {
            List<Rectangle> collisionRectangles = new List<Rectangle>();
            collisionRectangles.Add(burger.CollisionRectangle);
            foreach (TeddyBear bear in bears)
            {
                collisionRectangles.Add(bear.CollisionRectangle);
            }
            foreach (Projectile projectile in projectiles)
            {
                collisionRectangles.Add(projectile.CollisionRectangle);
            }
            foreach (Explosion explosion in explosions)
            {
                collisionRectangles.Add(explosion.CollisionRectangle);
            }
            return collisionRectangles;
        }

        /// <summary>
        /// Checks to see if the burger has just been killed
        /// </summary>
        private void CheckBurgerKill()
        {
            if (burger.Health == 0 && burgerDead == false)
            {
                burgerDeath.Play(1,0,0);
                burgerDead = true;
                
            }
        }

        #endregion
    }
}
