﻿using System;
using Sandbox;

namespace SWB_Base.Attachments
{
    public class Laser : OffsetAttachment
    {
        public override string Name => "Laser";
        public override string Description => "Aids target acquisition by projecting a beam onto the target that provides a visual reference point.";
        public override string[] Positives => new string[]
        {
        };

        public override string[] Negatives => new string[]
        {
            "Visibible to enemies"
        };

        public override StatModifier StatModifier => new StatModifier
        {
            Spread = -0.05f,
        };

        public override string EffectAttachment => "laser_start"; // Laser start point

        public string Particle { get; set; } = "particles/swb/laser/laser_small.vpcf";

        //public Color[] Colors { get; set; } = { Color.Green };

        public Color Color { get; set; } = Color.Red;
        public int Range { get; set; } = 35;
        public bool RainbowColor { get; set; }

        private Particles laserParticle;
        private WeaponBase weapon;
        private float rainbowI;

        public Laser()
        {
            Event.Register(this);
        }

        ~Laser()
        {
            Event.Unregister(this);
        }

        private void CreateParticle()
        {
            DestroyParticle();

            laserParticle = Particles.Create(Particle);
            laserParticle.SetPosition(3, Color);
        }

        private void DestroyParticle()
        {
            if (laserParticle != null)
            {
                laserParticle.Destroy(true);
                laserParticle = null;
            }
        }

        public override void OnEquip(WeaponBase weapon, AttachmentModel attachmentModel)
        {
            this.weapon = weapon;

            if (Host.IsClient)
            {
                CreateParticle();
            }
        }

        public override void OnUnequip(WeaponBase weapon)
        {
            this.weapon = null;

            if (Host.IsClient)
            {
                DestroyParticle();
            }
        }

        [Event.Frame]
        public void OnFrame()
        {
            // Destroy laser when dropped or if weapon owner switches weapon
            if (weapon != null && (weapon.Owner == null || (weapon.Owner != null && weapon.Owner != Local.Pawn && weapon.Owner.ActiveChild != weapon)))
            {
                this.weapon = null;
                DestroyParticle();
                return;
            }

            // Create lasers for other clients
            if (laserParticle == null && weapon == null)
            {
                foreach (var entity in Entity.All)
                {
                    // Find weapon with active laser that has no weapon assigned
                    if (entity is WeaponBase weapon && entity.Owner != null && entity.Owner != Local.Pawn)
                    {
                        var activeAttach = weapon.GetActiveAttachment(Name);
                        if (activeAttach == null || activeAttach.WorldAttachmentModel == null) continue;

                        // Attachment weapon found
                        this.weapon = weapon;
                        CreateParticle();

                        break;
                    }
                }
            }

            // Update laser properties
            if (laserParticle != null && weapon != null && weapon.IsValid)
            {
                var activeAttach = weapon.GetActiveAttachment(Name);
                if (activeAttach == null)
                {
                    // Delete laser for other clients
                    if (weapon.Owner != Local.Pawn)
                    {
                        this.weapon = null;
                        DestroyParticle();
                    }

                    return;
                };

                Transform? laserAttach;

                // Color
                if (RainbowColor)
                {
                    rainbowI += 0.002f;

                    if (rainbowI > 1)
                        rainbowI = 0;

                    laserParticle.SetPosition(3, ColorUtil.HSL2RGB(rainbowI, 0.5, 0.5));
                }

                laserParticle.EnableDrawing = !weapon.ShouldTuck();

                // Firstperson & Thirdperson
                if (Local.Pawn == weapon.Owner && weapon.IsFirstPersonMode)
                {
                    if (activeAttach.ViewAttachmentModel == null || !activeAttach.ViewAttachmentModel.IsValid) return;
                    laserAttach = activeAttach.ViewAttachmentModel.GetAttachment(EffectAttachment);
                }
                else
                {
                    if (activeAttach.WorldAttachmentModel == null || !activeAttach.WorldAttachmentModel.IsValid) return;
                    laserAttach = activeAttach.WorldAttachmentModel.GetAttachment(EffectAttachment);
                }

                var laserTrans = laserAttach.GetValueOrDefault();
                var laserStartPos = laserTrans.Position;

                laserParticle.SetPosition(0, laserStartPos);

                var owner = weapon.Owner;
                if (weapon.Owner == null) return;

                var tr = Trace.Ray(owner.EyePos, laserStartPos + laserTrans.Rotation.Forward * Range)
                                .Size(1.0f)
                                .Ignore(owner)
                                .UseHitboxes()
                                .Run();

                laserParticle.SetPosition(1, tr.EndPos);
            }
        }
    }

    public class SmallLaser : Laser
    {
        public override string Name => "CMR-207 Laser";
        public override string IconPath => "attachments/swb/tactical/laser_small/ui/icon.png";
        public override string ModelPath => "attachments/swb/tactical/laser_small/w_laser_small.vmdl";
    }

    public class SmallLaserRed : SmallLaser
    {
        public override string Name => "CMR-207 Laser (red)";
        public override string IconPath => "attachments/swb/tactical/laser_small/ui/icon_red.png";
    }
    public class SmallLaserBlue : SmallLaser
    {
        public override string Name => "CMR-207 Laser (blue)";
        public override string IconPath => "attachments/swb/tactical/laser_small/ui/icon_blue.png";
    }
    public class SmallLaserGreen : SmallLaser
    {
        public override string Name => "CMR-207 Laser (green)";
        public override string IconPath => "attachments/swb/tactical/laser_small/ui/icon_green.png";
    }
    public class SmallLaserRainbow : SmallLaser
    {
        public override string Name => "CMR-207 Laser (rainbow)";
        public override string IconPath => "attachments/swb/tactical/laser_small/ui/icon_rainbow.png";
    }
}
