using UnityEngine;

namespace ProjectBreachpoint
{
    public sealed class RecoilSystem : MonoBehaviour
    {
        private FirstPersonController firstPersonController;
        private WeaponData currentWeapon;
        private int recoilIndex;
        private float lastShotTime = -999f;
        private Vector2 visualRecoil;

        public int RecoilIndex
        {
            get { return recoilIndex; }
        }

        private void Awake()
        {
            firstPersonController = GetComponent<FirstPersonController>();
        }

        private void Update()
        {
            if (currentWeapon == null)
            {
                return;
            }

            if (Time.time - lastShotTime > currentWeapon.RecoilResetDelay)
            {
                recoilIndex = Mathf.Max(0, recoilIndex - Mathf.CeilToInt(Time.deltaTime * currentWeapon.RecoilRecoverySpeed * 4f));
            }

            visualRecoil = Vector2.Lerp(visualRecoil, Vector2.zero, Time.deltaTime * currentWeapon.RecoilRecoverySpeed);
        }

        public void SetWeapon(WeaponData weapon)
        {
            currentWeapon = weapon;
            recoilIndex = 0;
            visualRecoil = Vector2.zero;
        }

        public Vector2 ApplyShot(WeaponData weapon)
        {
            currentWeapon = weapon;
            Vector2 pattern = Vector2.zero;
            if (weapon.RecoilPattern != null && weapon.RecoilPattern.Length > 0)
            {
                pattern = weapon.RecoilPattern[Mathf.Min(recoilIndex, weapon.RecoilPattern.Length - 1)];
            }

            recoilIndex++;
            lastShotTime = Time.time;
            visualRecoil += pattern * weapon.CameraKick;

            if (firstPersonController != null)
            {
                firstPersonController.AddViewKick(pattern.x * weapon.CameraKick, pattern.y * weapon.CameraKick);
            }

            return pattern;
        }

        public float GetSprayPenalty()
        {
            if (currentWeapon == null)
            {
                return 0f;
            }

            return Mathf.Clamp01(recoilIndex / 12f) * currentWeapon.BaseSpread * 3f;
        }
    }
}
