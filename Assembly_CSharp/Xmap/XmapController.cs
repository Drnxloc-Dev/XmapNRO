﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Assembly_CSharp.Xmap
{
    public class XmapController : IActionListener
    {
        private const int TIME_DELAY_NEXTMAP = 1000;
        private const int TIME_DELAY_RENEXTMAP = 500;
        private const int PANEL_WAIT_DELAY = 100;
        private const int ID_ITEM_CAPSUAL_VIP = 194;
        private const int ID_ITEM_CAPSUAL = 193;

        private static Thread XmapThread;
        private static XmapController _Instance = new XmapController();

        public static void StartRunToMapId(int idMap)
        {
            if (XmapThread != null)
            {
                if (XmapThread.IsAlive)
                    GameScr.info1.addInfo("Xmap trước đó chưa kết thúc, đang ngắt luồng", 0);
                XmapThread.Abort();
            }

            XmapThread = new Thread(RunToMapId)
            {
                IsBackground = true
            };
            XmapThread.Start(idMap);
        }

        public static void UseCapsual()
        {
            if (Algorithm.HasCapsualVip())
            {
                Service.gI().useItem(0, 1, -1, ID_ITEM_CAPSUAL_VIP);
                return;
            }
            Service.gI().useItem(0, 1, -1, ID_ITEM_CAPSUAL);
        }

        public static void WaitPanelMapTrans()
        {
            while (!GameCanvas.panel.isShow || Pk9r.IsMapTransAsXmap)
            {
                Thread.Sleep(PANEL_WAIT_DELAY);
            }
        }

        public static void ShowXmapMenu()
        {
            MapConnection.LoadGroupMapsFromFile("TextData\\GroupMapsXmap.txt");

            MyVector myVector = new MyVector();
            foreach (var groupMap in MapConnection.GroupMaps)
            {
                myVector.addElement(new Command(groupMap.NameGroup, _Instance, 1, groupMap.IdMaps));
            }
            GameCanvas.menu.startAt(myVector, 3);
        }

        public static void ShowSelectMap(List<int> idMaps)
        {
            Pk9r.IsMapTransAsXmap = true;

            int len = idMaps.Count;
            GameCanvas.panel.mapNames = new string[len];
            GameCanvas.panel.planetNames = new string[len];
            for (int i = 0; i < len; i++)
            {
                string nameMap = TileMap.mapNames[idMaps[i]];
                GameCanvas.panel.mapNames[i] = idMaps[i] + ": " + nameMap;
                GameCanvas.panel.planetNames[i] = "Xmap by Phucprotein";
            }
            GameCanvas.panel.setTypeMapTrans();
            GameCanvas.panel.show();
        }

        public static void HideInfoDlg()
        {
            InfoDlg.hide();
        }

        private static void RunToMapId(object idMap)
        {
            try
            {
                LinkMaps linkMaps = MapConnection.GetLinkMaps();
                int idMapStart = TileMap.mapID;
                int idMapEnd = (int)idMap;

                List<int> way = Algorithm.FindWay(linkMaps, idMapStart, idMapEnd);

                if (way == null)
                {
                    GameScr.info1.addInfo("Không thể tìm thấy đường đi", 0);
                    return;
                }

                if (!RunWay(linkMaps, way))
                {
                    GameScr.info1.addInfo("Có lỗi xảy ra, đang thử lại", 0);
                    RunToMapId(idMap);
                    return;
                }

                GameScr.info1.addInfo("Xmap by Phucprotein", 0);
            }
            catch (Exception e)
            {
                GameScr.info1.addInfo(e.Message, 0);
            }
        }

        private static bool RunWay(LinkMaps linkMaps, List<int> way)
        {
            for (int i = 0; i < way.Count - 1; i++)
            {
                while (TileMap.mapID == way[i])
                {
                    if (Algorithm.CanNextMap())
                        NextMap(linkMaps, way[i + 1]);

                    Thread.Sleep(TIME_DELAY_RENEXTMAP);
                }
                Thread.Sleep(TIME_DELAY_NEXTMAP);
                if (TileMap.mapID != way[i + 1])
                {
                    return false;
                }
            }
            return true;
        }

        private static void NextMap(LinkMaps linkMaps, int idMapNext)
        {
            List<MapNext> mapNexts = Algorithm.GetMapNexts(linkMaps);
            if (mapNexts != null)
            {
                foreach (MapNext mapNext in mapNexts)
                {
                    if (mapNext.MapID == idMapNext)
                    {
                        NextMap(mapNext);
                        return;
                    }
                }
            }
            GameScr.info1.addInfo("Lỗi tại dữ liệu", 0);
        }

        private static void NextMap(MapNext mapNext)
        {
            switch (mapNext.Type)
            {
                case TypeMapNext.AutoWaypoint:
                    NextMapAutoWaypoint(mapNext);
                    break;

                case TypeMapNext.NpcMenu:
                    NextMapNpcMenu(mapNext);
                    break;

                case TypeMapNext.NpcPanel:
                    NextMapNpcPanel(mapNext);
                    break;

                case TypeMapNext.Position:
                    NextMapPosition(mapNext);
                    break;

                case TypeMapNext.Capsual:
                    NextMapCapsual(mapNext);
                    break;
            }
        }

        private static void NextMapAutoWaypoint(MapNext mapNext)
        {
            Waypoint waypoint = Algorithm.FindWaypoint(mapNext.MapID);
            if (waypoint != null)
                NextMapWaypoint(waypoint);
        }

        private static void NextMapNpcMenu(MapNext mapNext)
        {
            int idNpc = mapNext.Info[0];
            Service.gI().openMenu(idNpc);
            for (int i = 1; i < mapNext.Info.Length; i++)
            {
                int select = mapNext.Info[i];
                Service.gI().confirmMenu((short)idNpc, (sbyte)select);
            }
        }

        private static void NextMapNpcPanel(MapNext mapNext)
        {
            int idNpc = mapNext.Info[0];
            int selectMenu = mapNext.Info[1];
            int selectPanel = mapNext.Info[2];
            Service.gI().openMenu(idNpc);
            Service.gI().confirmMenu((short)idNpc, (sbyte)selectMenu);
            Service.gI().requestMapSelect(selectPanel);
        }

        private static void NextMapPosition(MapNext mapNext)
        {
            int xPos = mapNext.Info[0];
            int yPos = mapNext.Info[1];
            MoveMyChar(xPos, yPos);
            Service.gI().requestChangeMap();
        }

        private static void NextMapCapsual(MapNext mapNext)
        {
            Pk9r.SaveIdMapCapsualReturn();
            int index = mapNext.Info[0];
            Service.gI().requestMapSelect(index);
        }

        private static void NextMapWaypoint(Waypoint waypoint)
        {
            int x = Algorithm.GetPosWaypointX(waypoint);
            int y = Algorithm.GetPosWaypointY(waypoint);
            MoveMyChar(x, y);
            RequestChangeMap(waypoint);
        }

        private static void MoveMyChar(int x, int y)
        {
            Char.myCharz().cx = x;
            Char.myCharz().cy = y;
            Service.gI().charMove();
            Char.myCharz().cx = x;
            Char.myCharz().cy = y + 1;
            Service.gI().charMove();
            Char.myCharz().cx = x;
            Char.myCharz().cy = y;
            Service.gI().charMove();
        }

        private static void RequestChangeMap(Waypoint waypoint)
        {
            if (waypoint.isOffline)
            {
                Service.gI().getMapOffline();
                return;
            }
            Service.gI().requestChangeMap();
        }

        public void perform(int idAction, object p)
        {
            switch (idAction)
            {
                case 1:
                    List<int> idMaps = (List<int>)p;
                    ShowSelectMap(idMaps);
                    break;
            }
        }
    }
}
