using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Neolithic
{
    class FailedAttempt
    {
        public long LastTryMs;
        public int Count;
    }

    public class FixedAiTaskSeekFoodAndEat : AiTaskBase
    {
        AssetLocation eatSound;

        POIRegistry porregistry;
        IAnimalFoodSource target;
        public AnimationMetaData nlAnimMeta;

        float moveSpeed = 0.02f;
        long stuckatMs = 0;
        bool nowStuck = false;

        float eatTime = 1f;

        float eatTimeNow = 0;
        bool soundPlayed = false;
        bool doConsumePortion = true;
        bool doOnce = true;
        public string eatAnimation = "eat";

        Dictionary<IAnimalFoodSource, FailedAttempt> failedSeekTargets = new Dictionary<IAnimalFoodSource, FailedAttempt>();

        public FixedAiTaskSeekFoodAndEat(EntityAgent entity) : base(entity)
        {
            porregistry = entity.Api.ModLoader.GetModSystem<POIRegistry>();
        }

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            base.LoadConfig(taskConfig, aiConfig);

            if (taskConfig["eatSound"] != null)
            {
                string eatsoundstring = taskConfig["eatSound"].AsString(null);
                if (eatsoundstring != null) eatSound = new AssetLocation(eatsoundstring).WithPathPrefix("sounds/");
            }

            if (taskConfig["movespeed"] != null)
            {
                moveSpeed = taskConfig["movespeed"].AsFloat(0.02f);
            }

            if (taskConfig["eatTime"] != null)
            {
                eatTime = taskConfig["eatTime"].AsFloat(1.5f);
            }

            if (taskConfig["doConsumePortion"] != null)
            {
                doConsumePortion = taskConfig["doConsumePortion"].AsBool(true);
            }

            if (taskConfig["eatanimation"].Exists)
            {
                nlAnimMeta = new AnimationMetaData()
                {
                    Code = taskConfig["eatanimation"].AsString()?.ToLowerInvariant(),
                    Animation = taskConfig["eatanimation"].AsString()?.ToLowerInvariant(),
                    AnimationSpeed = taskConfig["eatanimationspeed"].AsFloat(1f)
                }.Init();
            }
            

        }

        IPointOfInterest nearestPoi;
        bool shouldSeek = true;

        public override bool ShouldExecute()
        {
            if (entity.World.Rand.NextDouble() < 0.005) return false;
            if (cooldownUntilMs > entity.World.ElapsedMilliseconds) return false;
            if (cooldownUntilTotalHours > entity.World.Calendar.TotalHours) return false;
            if (whenInEmotionState != null && !entity.HasEmotionState(whenInEmotionState)) return false;
            if (whenNotInEmotionState != null && entity.HasEmotionState(whenNotInEmotionState)) return false;

            EntityBehaviorMultiply bh = entity.GetBehavior<EntityBehaviorMultiply>();
            if (bh != null && !bh.ShouldEat) return false;

            if (shouldSeek) entity.World.RegisterCallback(SeekFoodSources, 5000);
            
            return nearestPoi != null;
        }

        public void SeekFoodSources(float dt)
        {
            shouldSeek = true;
            nearestPoi = porregistry.GetNearestPoi(entity.ServerPos.XYZ, 32, (poi) =>
            {
                if (poi.Type != "food") return false;

                if ((target = poi as IAnimalFoodSource)?.IsSuitableFor(entity) == true)
                {
                    FailedAttempt attempt;
                    failedSeekTargets.TryGetValue(target, out attempt);
                    doOnce = true;
                    if (attempt == null || (attempt.Count < 4 || attempt.LastTryMs < world.ElapsedMilliseconds - 60000)) return true;
                }

                return false;
            });
        }

        public float MinDistanceToTarget()
        {
            return System.Math.Max(0.8f, (entity.CollisionBox.X2 - entity.CollisionBox.X1) / 2 + 0.05f);
        }

        public override void StartExecute()
        {
            base.StartExecute();
            stuckatMs = -9999;
            nowStuck = false;
            soundPlayed = false;
            eatTimeNow = 0;
            pathTraverser.GoTo(target.Position, moveSpeed, MinDistanceToTarget(), OnGoalReached, OnStuck);
        }

        public override bool ContinueExecute(float dt)
        {
            Vec3d pos = target.Position;

            pathTraverser.CurrentTarget.X = pos.X;
            pathTraverser.CurrentTarget.Y = pos.Y;
            pathTraverser.CurrentTarget.Z = pos.Z;

            Cuboidd targetBox = entity.CollisionBox.ToDouble().Translate(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
            double distance = targetBox.ShortestDistanceFrom(pos);

            float minDist = MinDistanceToTarget();

            if (distance < minDist)
            {
                eatTimeNow += dt;

                if (eatTimeNow > eatTime * 0.75f && !soundPlayed)
                {
                    soundPlayed = true;
                    if (eatSound != null) entity.World.PlaySoundAt(eatSound, entity, null, true, 16, 1);
                }


                if (eatTimeNow >= eatTime)
                {
                    ITreeAttribute tree = entity.WatchedAttributes.GetTreeAttribute("hunger");
                    if (tree == null) entity.WatchedAttributes["hunger"] = tree = new TreeAttribute();

                    if (doConsumePortion)
                    {
                        float sat = target.ConsumeOnePortion();
                        tree.SetFloat("saturation", sat + tree.GetFloat("saturation", 0));
                        entity.WatchedAttributes.MarkPathDirty("hunger");
                    }

                    failedSeekTargets.Remove(target);

                    return false;
                }
            }


            if (nowStuck && entity.World.ElapsedMilliseconds > stuckatMs + eatTime * 1000)
            {
                return false;
            }
            return true;
        }


        float GetSaturation()
        {
            ITreeAttribute tree = entity.WatchedAttributes.GetTreeAttribute("hunger");
            if (tree == null) entity.WatchedAttributes["hunger"] = tree = new TreeAttribute();

            return tree.GetFloat("saturation");
        }


        public override void FinishExecute(bool cancelled)
        {
            base.FinishExecute(cancelled);
            pathTraverser.Stop();

            if (cancelled)
            {
                cooldownUntilTotalHours = 0;
            }
            if (animMeta != null && nlAnimMeta != null)
            {
                entity.StopAnimation(nlAnimMeta.Code);
            }
        }



        private void OnStuck()
        {
            stuckatMs = entity.World.ElapsedMilliseconds;
            nowStuck = true;

            FailedAttempt attempt = null;
            failedSeekTargets.TryGetValue(target, out attempt);
            if (attempt == null)
            {
                failedSeekTargets[target] = attempt = new FailedAttempt();
            }

            attempt.Count++;
            attempt.LastTryMs = world.ElapsedMilliseconds;

        }

        private void OnGoalReached()
        {
            pathTraverser.Active = true;

            failedSeekTargets.Remove(target);
            if (animMeta != null && nlAnimMeta != null && doOnce)
            {
                entity.StopAnimation(animMeta.Code);
                entity.StartAnimation(nlAnimMeta.ToString());
                doOnce = false;
            }
        }


    }
}
