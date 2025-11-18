using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Managers
{
    class MinimapManager : Singleton<MinimapManager>
    {
        public UIMinimap minimap;

        private Collider minimapBoundingBox;
        public Collider MinimapBoundingBox
        {
            get { return minimapBoundingBox; }
        }

        /// <summary>
        /// 角色位置
        /// </summary>
        public Transform PlayerTransform
        {
            get
            {
                if (User.Instance.CurrentCharacterObject == null)
                {
                    return null;
                }
                return User.Instance.CurrentCharacterObject.transform;
            }
        }

        /// <summary>
        /// 加载小地图资源
        /// </summary>
        /// <returns></returns>
        public Sprite LoadCurrentMinimap()
        {
            return Resloader.Load<Sprite>("UI/Minimap/" + User.Instance.CurrentMapData.MiniMap);
        }

        /// <summary>
        /// 更新小地图(对外接口）
        /// </summary>
        /// <param name="minimapBoundingBox"></param>
        public void UpdateMinimap(Collider minimapBoundingBox)
        {
            this.minimapBoundingBox = minimapBoundingBox;
            if(this.minimap != null)
            {
                this.minimap.UpdateMap();
            }
        }
    }
}
