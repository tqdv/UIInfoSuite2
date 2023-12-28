using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Quests;
using StardewValley.WorldMaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Options;

namespace UIInfoSuite2.UIElements
{
    internal class LocationOfTownsfolk : IDisposable
    {
        #region Properties
        private SocialPage _socialPage = null!;
        private string[] _friendNames = null!;
        private List<NPC> _townsfolk = new();
        private List<OptionsCheckbox> _checkboxes = new();

        private readonly ModOptions _options;
        private readonly IModHelper _helper;

        private const int SocialPanelWidth = 190;
        private const int SocialPanelXOffset = 160;

        private static readonly Dictionary<string, Vector2> _mapLocations = new()
        {
            { "HarveyRoom", new Vector2(677, 304) },
            { "BathHouse_Pool", new Vector2(576, 60) },
            { "WizardHouseBasement", new Vector2(196, 352) },
            { "BugLand", new Vector2(0, 0) },
            { "Desert", new Vector2(75, 40) },
            { "Cellar", new Vector2(470, 260) },
            { "JojaMart", new Vector2(872, 280) },
            { "LeoTreeHouse", new Vector2(744, 128) },
            { "Tent", new Vector2(784, 128) },
            { "HaleyHouse", new Vector2(652, 408) },
            { "Hospital", new Vector2(677, 304) },
            { "FarmHouse", new Vector2(470, 260) },
            { "Farm", new Vector2(470, 260) },
            { "ScienceHouse", new Vector2(732, 148) },
            { "ManorHouse", new Vector2(768, 395) },
            { "AdventureGuild", new Vector2(0, 0) },
            { "SeedShop", new Vector2(696, 296) },
            { "Blacksmith", new Vector2(852, 388) },
            { "JoshHouse", new Vector2(740, 320) },
            { "SandyHouse", new Vector2(40, 115) },
            { "Tunnel", new Vector2(0, 0) },
            { "CommunityCenter", new Vector2(692, 204) },
            { "Backwoods", new Vector2(460, 156) },
            { "ElliottHouse", new Vector2(826, 550) },
            { "SebastianRoom", new Vector2(732, 148) },
            { "BathHouse_Entry", new Vector2(576, 60) },
            { "Greenhouse", new Vector2(370, 270) },
            { "Sewer", new Vector2(380, 596) },
            { "WizardHouse", new Vector2(196, 352) },
            { "Trailer", new Vector2(780, 360) },
            { "Trailer_Big", new Vector2(780, 360) },
            { "Forest", new Vector2(80, 272) },
            { "Woods", new Vector2(100, 272) },
            { "WitchSwamp", new Vector2(0, 0) },
            { "ArchaeologyHouse", new Vector2(892, 416) },
            { "FishShop", new Vector2(844, 608) },
            { "Saloon", new Vector2(714, 354) },
            { "LeahHouse", new Vector2(452, 436) },
            { "Town", new Vector2(680, 360) },
            { "Mountain", new Vector2(762, 154) },
            { "BusStop", new Vector2(516, 224) },
            { "Railroad", new Vector2(644, 64) },
            { "SkullCave", new Vector2(0, 0) },
            { "BathHouse_WomensLocker", new Vector2(576, 60) },
            { "Beach", new Vector2(790, 550) },
            { "BathHouse_MensLocker", new Vector2(576, 60) },
            { "Mine", new Vector2(880, 100) },
            { "WitchHut", new Vector2(0, 0) },
            { "AnimalShop", new Vector2(420, 392) },
            { "SamHouse", new Vector2(612, 396) },
            { "WitchWarpCave", new Vector2(0, 0) },
            { "Club", new Vector2(60, 92) },
            { "Sunroom", new Vector2(705, 304) }
        };
        #endregion

        #region Lifecycle
        public LocationOfTownsfolk(IModHelper helper, ModOptions options)
        {
            _helper = helper;
            _options = options;
        }

        public void ToggleShowNPCLocationsOnMap(bool showLocations)
        {
            InitializeProperties();
            _helper.Events.Display.MenuChanged -= OnMenuChanged;
            _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu_DrawSocialPageOptions;
            _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu_DrawNPCLocationsOnMap;
            _helper.Events.Input.ButtonPressed -= OnButtonPressed_ForSocialPage;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showLocations)
            {
                _helper.Events.Display.MenuChanged += OnMenuChanged;
                _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu_DrawSocialPageOptions;
                _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu_DrawNPCLocationsOnMap;
                _helper.Events.Input.ButtonPressed += OnButtonPressed_ForSocialPage;
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }

        public void Dispose()
        {
            ToggleShowNPCLocationsOnMap(false);
        }
        #endregion

        #region Event subscriptions
        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            InitializeProperties();
        }

        private void OnButtonPressed_ForSocialPage(object? sender, ButtonPressedEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu && (e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA || e.Button == SButton.ControllerX))
            {
                CheckSelectedBox(e);
            }
        }

        private void OnRenderedActiveMenu_DrawSocialPageOptions(object? sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 2)
            {
                DrawSocialPageOptions();
            }
        }
        private void OnRenderedActiveMenu_DrawNPCLocationsOnMap(object? sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 3)
            {
                DrawNPCLocationsOnMap(gameMenu);
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!e.IsOneSecond || (Context.IsSplitScreen && Context.ScreenId != 0))
                return;

            _townsfolk.Clear();

            foreach (var loc in Game1.locations)
            {
                foreach (var character in loc.characters)
                {
                    if (character.isVillager())
                        _townsfolk.Add(character);
                }
            }
        }
        #endregion

        #region Logic
        private void InitializeProperties()
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu)
            {
                foreach (var menu in gameMenu.pages)
                {
                    if (menu is SocialPage socialPage)
                    {
                        _socialPage = socialPage;
                        _friendNames = socialPage.GetAllNpcs().Select(n => n.Name).ToArray();
                        break;
                    }
                }

                _checkboxes.Clear();
                for (int i = 0; i < _friendNames.Length; i++)
                {
                    var friendName = _friendNames[i];
                    OptionsCheckbox checkbox = new OptionsCheckbox("", i);
                    if (Game1.player.friendshipData.ContainsKey(friendName))
                    {
                        // npc
                        checkbox.greyedOut = false;
                        checkbox.isChecked = _options.ShowLocationOfFriends.SafeGet(friendName, true);
                    }
                    else
                    {
                        // player
                        checkbox.greyedOut = true;
                        checkbox.isChecked = true;
                    }
                    _checkboxes.Add(checkbox);
                }
            }
        }

        private void CheckSelectedBox(ButtonPressedEventArgs e)
        {
            int slotPosition = (int)typeof(SocialPage)
                .GetField("slotPosition", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(_socialPage)!;

            for (int i = slotPosition; i < slotPosition + 5; ++i)
            {
                OptionsCheckbox checkbox = _checkboxes[i];
                var rect = new Rectangle(checkbox.bounds.X, checkbox.bounds.Y, checkbox.bounds.Width, checkbox.bounds.Height);
                if (e.Button == SButton.ControllerX)
                {
                    rect.Width += SocialPanelWidth + Game1.activeClickableMenu.width;
                }
                if (rect.Contains((int)Utility.ModifyCoordinateForUIScale(Game1.getMouseX()), (int)Utility.ModifyCoordinateForUIScale(Game1.getMouseY())) &&
                    !checkbox.greyedOut)
                {
                    checkbox.isChecked = !checkbox.isChecked;
                    _options.ShowLocationOfFriends[_friendNames[checkbox.whichOption]] = checkbox.isChecked;
                    Game1.playSound("drumkit6");
                }
            }
        }

        private void DrawSocialPageOptions()
        {
            Game1.drawDialogueBox(Game1.activeClickableMenu.xPositionOnScreen - SocialPanelXOffset, Game1.activeClickableMenu.yPositionOnScreen,
                SocialPanelWidth, Game1.activeClickableMenu.height, false, true);

            int slotPosition = (int)typeof(SocialPage)
                .GetField("slotPosition", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(_socialPage)!;
            int yOffset = 0;

            for (int i = slotPosition; i < slotPosition + 5 && i < _friendNames.Length; ++i)
            {
                OptionsCheckbox checkbox = _checkboxes[i];
                checkbox.bounds.X = Game1.activeClickableMenu.xPositionOnScreen - 60;

                checkbox.bounds.Y = Game1.activeClickableMenu.yPositionOnScreen + 130 + yOffset;

                checkbox.draw(Game1.spriteBatch, 0, 0);
                yOffset += 112;
                Color color = checkbox.isChecked ? Color.White : Color.Gray;

                Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(checkbox.bounds.X - 50, checkbox.bounds.Y), new Rectangle(80, 0, 16, 16),
                    color, 0.0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);

                if (yOffset != 560)
                {
                    // draw seperator line
                    Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(checkbox.bounds.X - 50, checkbox.bounds.Y + 72, SocialPanelWidth / 2 - 6, 4), Color.SaddleBrown);
                    Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(checkbox.bounds.X - 50, checkbox.bounds.Y + 76, SocialPanelWidth / 2 - 6, 4), Color.BurlyWood);
                }
                if (!Game1.options.hardwareCursor)
                {
                    Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.getMouseX(), Game1.getMouseY()),
                        Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, Game1.mouseCursor, 16, 16),
                        Color.White, 0.0f, Vector2.Zero, Game1.pixelZoom + (Game1.dialogueButtonScale / 150.0f), SpriteEffects.None, 1f);
                }

                if (checkbox.bounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
                    IClickableMenu.drawHoverText(Game1.spriteBatch, "Track on map", Game1.dialogueFont);
            }
        }

        private void DrawNPCLocationsOnMap(GameMenu gameMenu)
        {
            List<string> namesToShow = new List<string>();
            foreach (var character in _townsfolk)
            {
                try
                {
                    bool shouldDrawCharacter = Game1.player.friendshipData.ContainsKey(character.Name) && _options.ShowLocationOfFriends.SafeGet(character.Name, true) && character.id != -1;
                    if (shouldDrawCharacter)
                    {
                        DrawNPC(character, namesToShow);
                    }
                }
                catch (Exception ex)
                {
                    ModEntry.MonitorObject.Log(ex.Message + Environment.NewLine + ex.StackTrace, LogLevel.Error);
                }
            }
            DrawNPCNames(namesToShow);

            //The cursor needs to show up in front of the character faces
            Tools.DrawMouseCursor();

            var hoverText = ((MapPage)(gameMenu.pages[gameMenu.currentTab])).hoverText;
            IClickableMenu.drawHoverText(Game1.spriteBatch, hoverText, Game1.smallFont);
        }

        private static void DrawNPC(NPC character, List<string> namesToShow)
        {
            Vector2? location = GetMapCoordinatesForNPC(character);
            if (location is null)
            {
                return;
            }

            Rectangle headShot = character.GetHeadShot();
            int xBase = Game1.activeClickableMenu.xPositionOnScreen - 158;
            int yBase = Game1.activeClickableMenu.yPositionOnScreen - 40;
            var offsetLocation = location.Value + new Vector2(xBase, yBase);

            Color color = character.CurrentDialogue.Count <= 0 ? Color.Gray : Color.White;
            float headShotScale = 2f;
            Game1.spriteBatch.Draw(character.Sprite.Texture, offsetLocation, new Rectangle?(headShot),
                color, 0.0f, Vector2.Zero, headShotScale, SpriteEffects.None, 1f);

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            if (mouseX >= offsetLocation.X && mouseX - offsetLocation.X <= headShot.Width * headShotScale
             && mouseY >= offsetLocation.Y && mouseY - offsetLocation.Y <= headShot.Height * headShotScale)
            {
                namesToShow.Add(character.displayName);
            }

            DrawQuestsForNPC(character, (int)offsetLocation.X, (int)offsetLocation.Y);
        }

        private static Vector2? GetMapCoordinatesForNPC(NPC character)
        {
            return (Game1.player.currentLocation is IslandLocation ? GetIslandMapCoordinatesForNPC(character) : GetValleyMapCoordinatesForNPC(character));
        }

        private static Vector2? GetIslandMapCoordinatesForNPC(NPC character)
        {
            // The main valley map has an inset for GI, but the valley map has no such inset.  So characters just don't get drawn on that map.
            if (!(character.currentLocation is IslandLocation))
            {
                return null;
            }

            // Adapted from MapPage.drawMiniPortraits

            // this line is From MapPage.GetNormalizedPlayerTile -- looks irrelevant, really.
            var normalizedTile = new Point(Math.Max(0, character.TilePoint.X), Math.Max(0, character.TilePoint.Y));
            MapAreaPosition mapAreaPosition = WorldMapManager.GetPositionData(character.currentLocation, normalizedTile);

            var mapPosition = WorldMapManager.GetPositionData(character.currentLocation, normalizedTile) ?? WorldMapManager.GetPositionData(Game1.getFarm(), Point.Zero);
            var mapRegion = mapPosition.Region;
            var mapBounds = mapRegion.GetMapPixelBounds();

            //if (mapAreaPosition != null && !(mapAreaPosition.Region.Id != mapRegion.Id))
            //{
                Vector2 mapPixelPosition = mapAreaPosition.GetMapPixelPosition(character.currentLocation, normalizedTile);
                // mapPixelPosition = new Vector2(mapPixelPosition.X + (float)mapBounds.X - 32f, mapPixelPosition.Y + (float)mapBounds.Y - 32f);
            //}

            return mapPixelPosition;
        }

        private static Vector2 GetValleyMapCoordinatesForNPC(NPC character)
        {
            string locationName = character.currentLocation?.Name ?? character.DefaultMap;

            // Ginger Island
            if (character.currentLocation is IslandLocation)
            {
                return new Vector2(1104, 658);
            }

            // Scale Town and Forest
            if (locationName == "Town" || locationName == "Forest")
            {
                int xStart = locationName == "Town" ? 595 : 183;
                int yStart = locationName == "Town" ? 163 : 378;
                int areaWidth = locationName == "Town" ? 345 : 319;
                int areaHeight = locationName == "Town" ? 330 : 261;

                xTile.Map map = character.currentLocation!.Map;

                float xScale = areaWidth / (float)map.DisplayWidth;
                float yScale = areaHeight / (float)map.DisplayHeight;

                float scaledX = character.position.X * xScale;
                float scaledY = character.position.Y * yScale;
                int xPos = (int)scaledX + xStart;
                int yPos = (int)scaledY + yStart;
                return new Vector2(xPos, yPos);
            }

            // Other known locations
            return _mapLocations.SafeGet(locationName, new Vector2(0, 0));
        }

        private static void DrawQuestsForNPC(NPC character, int x, int y)
        {
            foreach (var quest in Game1.player.questLog.Where(q => q.accepted.Value && q.dailyQuest.Value && !q.completed.Value))
            {
                if ((quest is ItemDeliveryQuest idq && idq.target.Value == character.Name)
                 || (quest is SlayMonsterQuest smq && smq.target.Value == character.Name)
                 || (quest is FishingQuest fq && fq.target.Value == character.Name)
                 || (quest is ResourceCollectionQuest rq && rq.target.Value == character.Name))
                {
                    Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(x + 10, y - 12), new Rectangle(394, 495, 4, 10),
                        Color.White, 0.0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);
                }
            }
        }

        private static void DrawNPCNames(List<string> namesToShow)
        {
            if (namesToShow.Count == 0)
                return;

            StringBuilder text = new StringBuilder();
            int longestLength = 0;
            foreach (string name in namesToShow)
            {
                text.AppendLine(name);
                longestLength = Math.Max(longestLength, (int)Math.Ceiling(Game1.smallFont.MeasureString(name).Length()));
            }

            int windowHeight = Game1.smallFont.LineSpacing * namesToShow.Count + 25;
            Vector2 windowPos = new Vector2(Game1.getMouseX() + 40, Game1.getMouseY() - windowHeight);
            IClickableMenu.drawTextureBox(Game1.spriteBatch, (int)windowPos.X, (int)windowPos.Y,
                longestLength + 30, Game1.smallFont.LineSpacing * namesToShow.Count + 25, Color.White);

            Game1.spriteBatch.DrawString(Game1.smallFont, text, new Vector2(windowPos.X + 17, windowPos.Y + 17), Game1.textShadowColor);

            Game1.spriteBatch.DrawString(Game1.smallFont, text, new Vector2(windowPos.X + 15, windowPos.Y + 15), Game1.textColor);
        }
        #endregion
    }
}
