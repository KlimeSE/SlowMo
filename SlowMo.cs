using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.Plugins;
using VRage.Render.Particles;
using VRage.Utils;

namespace SlowMo
{
    public enum SlowdownState
    {
        Idle,
        GoToSlow,
        Slow,
        GoToIdle
    }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SlowMoSessionComp : MySessionComponentBase
    {
        SlowdownState currentSlowState = SlowdownState.Idle;
        float simSlow = 0.25f;
        bool validInputThisTick = false;
        int timer = 0;

        Dictionary<uint, MyParticleEffectData> particleStore = new Dictionary<uint, MyParticleEffectData>();

        MySoundPair timeWarpDown;
        MySoundPair timeWarpUp;

        MyEntity3DSoundEmitter emitter;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.Utilities.MessageEntered += MessageEntered;
        }

        public override void BeforeStart()
        {
            timeWarpDown = new MySoundPair("TimeWarpDown");
            timeWarpUp = new MySoundPair("TimeWarpUp");
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE)
            {
                return;
            }

            if (ValidInput())
            {
                validInputThisTick = true;
            }
            else
            {
                validInputThisTick = false;
            }

            if (validInputThisTick && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.F2))
            {
                if (currentSlowState == SlowdownState.Idle)
                {
                    currentSlowState = SlowdownState.GoToSlow;
                }
                else
                {
                    currentSlowState = SlowdownState.GoToIdle;
                }
            }

            if (currentSlowState == SlowdownState.Idle)
            {

            }

            if (currentSlowState == SlowdownState.GoToSlow)
            {
                if (MyAPIGateway.Session?.Player?.Character != null)
                {
                    var charac = MyAPIGateway.Session.Player.Character;
                    MyEntity charent = charac as MyEntity;
                    emitter = new MyEntity3DSoundEmitter(charent);
                    emitter.SetPosition(MyAPIGateway.Session.Camera.WorldMatrix.Translation);
                    emitter.PlaySound(timeWarpDown);
                }

                MyFakes.SIMULATION_SPEED = simSlow;
                currentSlowState = SlowdownState.Slow;
            }

            if (currentSlowState == SlowdownState.Slow)
            {
                foreach (var effect in MyParticlesManager.Effects)
                {
                    if (!particleStore.ContainsKey(effect.Id))
                    {
                        particleStore.Add(effect.Id, effect.Data);

                        var gens = effect.Data.GetGenerations();
                        foreach (var gen in gens)
                        {
                            gen.Life.SetValue(gen.Life.GetValue() * (1 / simSlow));
                            gen.AnimationFrameTime.SetValue(gen.AnimationFrameTime.GetValue() * (1/simSlow));
                        }
                        effect.Data.SetDirty();
                    }
                }
            }

            if (currentSlowState == SlowdownState.GoToIdle)
            {
                if (MyAPIGateway.Session?.Player.Character != null)
                {
                    var charac = MyAPIGateway.Session.Player.Character;
                    MyEntity charent = charac as MyEntity;
                    emitter = new MyEntity3DSoundEmitter(charent);
                    emitter.SetPosition(MyAPIGateway.Session.Camera.WorldMatrix.Translation);
                    emitter.PlaySound(timeWarpUp);
                }

                MyFakes.SIMULATION_SPEED = 1;

                foreach (var effect in MyParticlesManager.Effects)
                {
                    if (particleStore.ContainsKey(effect.Id))
                    {
                        var gens = effect.Data.GetGenerations();
                        foreach (var gen in gens)
                        {
                            gen.Life.SetValue(gen.Life.GetValue() * simSlow);
                            gen.AnimationFrameTime.SetValue(gen.AnimationFrameTime.GetValue() * simSlow);
                        }
                        effect.Data.SetDirty();
                    }
                }

                particleStore.Clear();
                currentSlowState = SlowdownState.Idle;
            }

            timer += 1;
        }

        private void MessageEntered(string messageText, ref bool sendToOthers)
        {
            if (messageText.StartsWith("/slow"))
            {
                var words = messageText.Split(' ').ToList();
                if (words != null && words.Count == 2)
                {
                    float tmpSimSlow = 1f;
                    if (float.TryParse(words[1], out tmpSimSlow))
                    {
                        simSlow = tmpSimSlow;
                        MyAPIGateway.Utilities.ShowMessage("", $"Slow motion set to: {simSlow}");
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("", "Incorrect format. Example: /slow 0.5");
                    }
                }
                sendToOthers = false;
            }
        }

        private bool ValidInput()
        {
            if (MyAPIGateway.Session.CameraController != null && !MyAPIGateway.Gui.ChatEntryVisible && !MyAPIGateway.Gui.IsCursorVisible
                && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.None)
            {
                return true;
            }
            return false;
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= MessageEntered;
            MyFakes.SIMULATION_SPEED = 1;
        }
    }

    public class SlowMo : IPlugin, IDisposable
    {
        public void Init(object gameInstance)
        {

        }

        public void Update()
        {
            
        }

        public void Dispose()
        {
            MyFakes.SIMULATION_SPEED = 1;
        }
    }
}
