using UnityEngine;
using System.Collections;
using packt.FoodyGo.Mapping;
using packt.FoodyGo.Services;

namespace packt.FoodyGO.Mapping
{
    [AddComponentMenu("Mapping/GoogleMapTile")]
    public class GoogleMapTile : MonoBehaviour
	{
		public enum MapType
		{
			RoadMap,
			Satellite,
			Terrain,
			Hybrid
		}

        private const string GOOGLE_MAPS_URL = "http://ditu.googleapis.cn/maps/api/staticmap";
        //private const string GOOGLE_MAPS_URL = "http://api.map.baidu.com/";

		public int zoomLevel = 1;
		public MapType mapType = MapType.RoadMap;
		public int size = 640;
		public bool doubleResolution = true;

        public GPSLocationService gpsLocationService;

        public MapLocation worldCenterLocation;
        public MapLocation tileCenterLocation;

        public Vector2 TileOffset;
        public Vector2 TopLeftCorner;
        public Vector2 BottomRightCorner;

        private double lastGPSUpdate;

		// Use this for initialization
		void Start ()
		{
			RefreshMapTile ();
		}
	
		// Update函数每一帧会调用一次
		void Update ()
		{
            //检查是否获得了新的位置
            if (gpsLocationService != null &&
                gpsLocationService.IsServiceStarted && 
                lastGPSUpdate < gpsLocationService.Timestamp)
            {
                lastGPSUpdate = gpsLocationService.Timestamp;
                worldCenterLocation.Latitude = gpsLocationService.Latitude;
                worldCenterLocation.Longitude = gpsLocationService.Longitude;
                print("GoogleMapTile refreshing map texture");
                RefreshMapTile();
            }
		}

		public void RefreshMapTile() {
			
			StartCoroutine(_RefreshMapTile());
		}

		IEnumerator _RefreshMapTile ()
		{   
            //找到瓦片中心的纬度和经度
			tileCenterLocation.Latitude = GoogleMapUtils.adjustLatByPixels(worldCenterLocation.Latitude, (int)(size * 1 * TileOffset.y), zoomLevel);
			tileCenterLocation.Longitude = GoogleMapUtils.adjustLonByPixels(worldCenterLocation.Longitude, (int)(size * 1 * TileOffset.x), zoomLevel);

			var url = GOOGLE_MAPS_URL;
            //构造查询地图瓦片的字符串参数
			var queryString = "";

			queryString += "center=" + WWW.UnEscapeURL (string.Format ("{0},{1}", tileCenterLocation.Latitude, tileCenterLocation.Longitude));
			queryString += "&zoom=" + zoomLevel.ToString ();
			queryString += "&size=" + WWW.UnEscapeURL (string.Format ("{0}x{0}", size));
			queryString += "&scale=" + (doubleResolution ? "2" : "1");
			queryString += "&maptype=" + mapType.ToString ().ToLower ();
			queryString += "&format=" + "png";

            //styles
            queryString += "&style=element:geometry|invert_lightness:true|weight:3.1|hue:0x00ffd5";
            queryString += "&style=element:labels|visibility:off";

            var usingSensor = false;
#if MOBILE_INPUT
            usingSensor = Input.location.isEnabledByUser && Input.location.status == LocationServiceStatus.Running;
#endif

			queryString += "&sensor=" + (usingSensor ? "true" : "false");

			//set map bounds rect
			TopLeftCorner.x = GoogleMapUtils.adjustLonByPixels(tileCenterLocation.Longitude, -size, zoomLevel);
			TopLeftCorner.y = GoogleMapUtils.adjustLatByPixels(tileCenterLocation.Latitude, size, zoomLevel);

			BottomRightCorner.x = GoogleMapUtils.adjustLonByPixels(tileCenterLocation.Longitude, size, zoomLevel);
			BottomRightCorner.y = GoogleMapUtils.adjustLatByPixels(tileCenterLocation.Latitude, -size, zoomLevel);

            print(string.Format("Tile {0}x{1} requested with {2}", TileOffset.x, TileOffset.y, queryString));

            //var req = new WWW(url + "?" + queryString);
            //var req = new WWW("https://maps.googleapis.com/maps/api/staticmap?center=50.917316,-114.080923&zoom=17&format=png&sensor=false&size=640x640&scale=2&maptype=roadmap&style=feature:landscape.man_made|visibility:on|invert_lightness:true");
            //在这里更改地址，以显示静态地图
            var req = new WWW("http://api.map.baidu.com/staticimage?center=116.988908,36.686075&width=500&height=500&zoom=19");
            //等待服务器返回
            yield return req;
            //先释放旧的贴图
            Destroy(GetComponent<Renderer>().material.mainTexture);
            //出错检查
            if (req.error != null)
            {
                print(string.Format("Tile {0}x{1} : exception={2} textured", TileOffset.x, TileOffset.y, req.error));
            }
            else
            {
                //没有错误，绘制图像
                //返回的图像作为瓦片地图
                GetComponent<Renderer>().material.mainTexture = req.texture;
                print(string.Format("Tile {0}x{1} textured", TileOffset.x, TileOffset.y));
            }
        }
	}
}
