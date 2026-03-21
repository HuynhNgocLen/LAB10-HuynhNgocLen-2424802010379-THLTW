using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HuynhNgocLen.SachOnline.Models;

namespace HuynhNgocLen.SachOnline.Models
{
    public class GioHang
    {
        SachOnlineEntities1 db = new SachOnlineEntities1();

        public int iMaSach { get; set; }
        public string sTenSach { get; set; }
        public string sAnhBia { get; set; }
        public double dDonGia { get; set; }
        public int iSoLuong { get; set; }
        public double dThanhTien
        {
            get { return iSoLuong * dDonGia; }
        }

        public GioHang(int ms)
        {
            iMaSach = ms;
            SACH sach = db.SACHes.Single(n => n.MaSach == iMaSach);
            if (sach != null)
            {
                sTenSach = sach.TenSach;
                sAnhBia = sach.AnhBia;
                dDonGia = double.Parse(sach.GiaBan.ToString());
                iSoLuong = 1;
            }
        }
    }
}