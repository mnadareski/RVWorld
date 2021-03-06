﻿using Compress.Utils;
using FileInfo = RVIO.FileInfo;
using FileStream = RVIO.FileStream;

// UInt16 = ushort
// UInt32 = uint
// ULong = ulong

namespace Compress.ZipFile
{
    public partial class Zip
    {
        private ulong _centerDirStart;
        private ulong _centerDirSize;
        private ulong _endOfCenterDir64;

        public ZipReturn ZipFileCreate(string newFilename)
        {
            if (ZipOpen != ZipOpenType.Closed)
            {
                return ZipReturn.ZipFileAlreadyOpen;
            }

            DirUtil.CreateDirForFile(newFilename);
            _zipFileInfo = new FileInfo(newFilename);

            int errorCode = FileStream.OpenFileWrite(newFilename, out _zipFs);
            if (errorCode != 0)
            {
                ZipFileClose();
                return ZipReturn.ZipErrorOpeningFile;
            }
            ZipOpen = ZipOpenType.OpenWrite;
            return ZipReturn.ZipGood;
        }

        private void zipFileCloseWrite()
        {
            _zip64 = false;
            bool lTrrntzip = true;

            _centerDirStart = (ulong)_zipFs.Position;

            using (CrcCalculatorStream crcCs = new CrcCalculatorStream(_zipFs, true))
            {
                foreach (LocalFile t in _localFiles)
                {
                    t.CenteralDirectoryWrite(crcCs);
                    _zip64 |= t.Zip64;
                    lTrrntzip &= t.TrrntZip;
                }

                crcCs.Flush();
                crcCs.Close();

                _centerDirSize = (ulong)_zipFs.Position - _centerDirStart;

                _fileComment = lTrrntzip ? ZipUtils.GetBytes("TORRENTZIPPED-" + crcCs.Crc.ToString("X8")) : new byte[0];
                ZipStatus = lTrrntzip ? ZipStatus.TrrntZip : ZipStatus.None;
            }
            _zip64 |= _centerDirStart >= 0xffffffff;
            _zip64 |= _centerDirSize >= 0xffffffff;
            _zip64 |= _localFiles.Count >= 0xffff;

            if (_zip64)
            {
                _endOfCenterDir64 = (ulong)_zipFs.Position;
                Zip64EndOfCentralDirWrite();
                Zip64EndOfCentralDirectoryLocatorWrite();
            }
            EndOfCentralDirWrite();

            _zipFs.SetLength(_zipFs.Position);
            _zipFs.Flush();
            _zipFs.Close();
            _zipFs.Dispose();
            _zipFileInfo = new FileInfo(_zipFileInfo.FullName);
            ZipOpen = ZipOpenType.Closed;
        }


        public void ZipFileCloseFailed()
        {
            switch (ZipOpen)
            {
                case ZipOpenType.Closed:
                    return;
                case ZipOpenType.OpenRead:
                    if (_zipFs != null)
                    {
                        _zipFs.Close();
                        _zipFs.Dispose();
                    }
                    break;
                case ZipOpenType.OpenWrite:
                    _zipFs.Flush();
                    _zipFs.Close();
                    _zipFs.Dispose();
                    if (_zipFileInfo != null)
                        RVIO.File.Delete(_zipFileInfo.FullName);
                    _zipFileInfo = null;
                    break;
            }

            ZipOpen = ZipOpenType.Closed;
        }

    }
}
