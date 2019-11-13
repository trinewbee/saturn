using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Xapi.Netdisk.Error
{
    public class XErrCode
    {
        public const uint ERROR_OK = 0;
        public const uint ERROR_FILE_NOT_FOUND = 2;
        public const uint ERROR_PATH_NOT_FOUND = 3;
        public const uint ERROR_ACCESS_DENIED = 5;
        public const uint ERROR_INVALID_HANDLE = 6;
        public const uint ERROR_WRITE_PROTECT = 19;
        public const uint ERROR_SHARING_VIOLATION = 32;
        public const uint ERROR_LOCK_VIOLATION = 33;
        public const uint ERROR_HANDLE_EOF = 38;
        public const uint ERROR_HANDLE_DISK_FULL = 39;
        public const uint ERROR_NOT_SUPPORTED = 50;
        public const uint ERROR_UNEXP_NET_ERR = 59;
        public const uint ERROR_INVALID_PARAMETER = 87;
        public const uint ERROR_INVALID_NAME = 123;
        public const uint ERROR_DIR_NOT_EMPTY = 145;
        public const uint ERROR_LOCK_FAILED = 167;
        public const uint ERROR_ALREADY_EXISTS = 183;
        public const uint ERROR_DELETE_PENDING = 303;
        public const uint ERROR_CAN_NOT_COMPLETE = 1003;
        public const uint ERROR_NO_MEDIA_IN_DRIVE = 1112;
        public const uint ERROR_TOO_MANY_LINKS = 1142;
        public const uint ERROR_CONNECTION_ABORTED = 1236;
        public const uint ERROR_DISK_QUOTA_EXCEEDED = 1295;
        public const uint ERROR_INTERNAL_ERROR = 1359;
        public const uint ERROR_FILE_CORRUPT = 1392;
        public const uint ERROR_BAD_PATHNAME = 161;
        public const uint ERROR_BAD_NET_RESP = 58;
        public const uint ERROR_FILE_CHECKED_OUT = 220;
    }



    public class XStat
    {
        public const string OK = "OK";

        public const string ERROR_SHARING_VIOLATION = "ERROR_SHARING_VIOLATION";
        public const string ERROR_ALREADY_EXISTS = "ERROR_ALREADY_EXISTS";
        public const string ERROR_ACCESS_DENIED = "ERROR_ACCESS_DENIED";
        public const string ERROR_INTERNAL_ERROR = "ERROR_INTERNAL_ERROR";
        public const string ERROR_PATH_NOT_FOUND = "ERROR_PATH_NOT_FOUND";
        public const string ERROR_FILE_NOT_FOUND = "ERR_FILE_NOT_FOUND";
        public const string ERROR_INVALID_PARAMETER = "ERROR_INVALID_PARAMETER";
        public const string ERROR_WRITE_PROTECT = "ERROR_WRITE_PROTECT";
        public const string ERROR_INVALID_HANDLE = "ERROR_INVALID_HANDLE"; //Reached the end of the file.
        public const string ERROR_LOCK_VIOLATION = "ERROR_LOCK_VIOLATION";
        public const string ERROR_HANDLE_EOF = "ERROR_HANDLE_EOF";
        public const string ERROR_HANDLE_DISK_FULL = "ERROR_HANDLE_DISK_FULL";
        public const string ERROR_NOT_SUPPORTED = "ERROR_NOT_SUPPORTED";
        public const string ERROR_UNEXP_NET_ERR = "ERROR_UNEXP_NET_ERR";
        public const string ERROR_INVALID_NAME = "ERROR_INVALID_NAME";
        public const string ERROR_DIR_NOT_EMPTY = "ERROR_DIR_NOT_EMPTY";
        public const string ERROR_LOCK_FAILED = "ERROR_LOCK_FAILED";
        public const string ERROR_DELETE_PENDING = "ERROR_DELETE_PENDING";
        public const string ERROR_CAN_NOT_COMPLETE = "ERROR_CAN_NOT_COMPLETE";
        public const string ERROR_NO_MEDIA_IN_DRIVE = "ERROR_NO_MEDIA_IN_DRIVE";
        public const string ERROR_TOO_MANY_LINKS = "ERROR_TOO_MANY_LINKS";
        public const string ERROR_CONNECTION_ABORTED = "ERROR_CONNECTION_ABORTED";
        public const string ERROR_DISK_QUOTA_EXCEEDED = "ERROR_DISK_QUOTA_EXCEEDED";
        public const string ERROR_FILE_CORRUPT = "ERROR_FILE_CORRUPT";
        public const string ERROR_BAD_PATHNAME = "ERROR_BAD_PATHNAME";
        public const string ERROR_BAD_NET_RESP = "ERROR_BAD_NET_RESP";
        public const string ERROR_FILE_CHECKED_OUT = "ERROR_FILE_CHECKED_OUT";

        public const string ERR_FILE_DIR_STRUCTURE_ERROR = "ERR_FILE_DIR_STRUCTURE_ERROR";


        //Fatal Error
        //{action:"raiseError", data={code:"ERR_TOKEN_EXPIRED", data:{message:"dddddd"}}}
        public const string ERR_TOKEN_EXPIRED = "ERR_TOKEN_EXPIRED";
        public const string ERR_TOKEN_NOT_FOUND = "ERR_TOKEN_NOT_FOUND";
        public const string ERR_TOKEN_UID_NOT_FOUND = "ERR_TOKEN_UID_NOT_FOUND";
        public const string ERR_TOKEN_UID_MISMATCHING = "ERR_TOKEN_UID_MISMATCHING";


        public const string LERR_ABORTED = "LERR_ABORTED";
        public const string LERR_CACHE_LOST = "LERR_CACHE_LOST";
        public const string LERR_IO_EXCEPTION = "LERR_IO_EXCEPTION";
        public const string LERR_CBFS_DRIVER_NOT_INSTALL = "LERR_CBFS_DRIVER_NOT_INSTALL";



        public static bool Success(string stat)
        {
            return OK == stat;
        }

        public static uint StatToCode(string stat)
        {
            uint code = XErrCode.ERROR_INTERNAL_ERROR;
            if (sCodes.TryGetValue(stat, out code))
            {
                return code;
            }
            return code;
        }

        //ERROR_BAD_ENVIRONMENT 10
        //ERROR_INVALID_ACCESS 12
        //ERROR_INVALID_DATA 13
        //ERROR_INVALID_DRIVE 15
        //ERROR_NOT_SAME_DEVICE 17
        //ERROR_NOT_READY 21
        //ERROR_BAD_COMMAND 22
        //ERROR_WRITE_FAULT 29
        //ERROR_READ_FAULT 30
        //ERROR_REM_NOT_LIST 51   //Windows cannot find the network path
        //ERROR_BAD_NETPATH 53 //The network path was not found.
        //ERROR_NETWORK_BUSY 54
        //ERROR_BAD_NET_RESP 58 //The specified server cannot perform the requested operation.
        //ERROR_NETWORK_ACCESS_DENIED 65
        //ERROR_CANNOT_MAKE 82 //The directory or file cannot be created.
        //ERROR_OUT_OF_STRUCTURES 84 //Storage to process this request is not available.
        //ERROR_DRIVE_LOCKED 108
        //ERROR_OPEN_FAILED 110 //The system cannot open the device or file specified.
        //ERROR_INVALID_TARGET_HANDLE 114 //The target internal file identifier is incorrect.
        //ERROR_DIR_NOT_ROOT 144 //The directory is not a subdirectory of the root directory.
        //ERROR_PATH_BUSY 148 //The path specified cannot be used at this time.
        //ERROR_BAD_PATHNAME 161//The specified path is invalid.
        //ERROR_BUSY 170 //The requested resource is in use.
        //ERROR_FILE_TOO_LARGE 223 //too large
        //ERROR_DIRECTORY 267 //The directory name is invalid.
        //ERROR_DELETE_PENDING 303 //The file cannot be opened because it is in the process of being deleted.
        //ERROR_DIRECTORY_NOT_SUPPORTED 336 //An operation is not supported on a directory.
        //ERROR_INVALID_ADDRESS 487 //Attempt to access invalid address.



        //public const string ERROR_INVALID_PATH = "ERROR_INVALID_PATH";


        static Dictionary<string, uint> sCodes = new Dictionary<string, uint>();
        static XStat()
        {
            sCodes[ERROR_SHARING_VIOLATION] = XErrCode.ERROR_SHARING_VIOLATION;
            sCodes[ERROR_ALREADY_EXISTS] = XErrCode.ERROR_ALREADY_EXISTS;
            sCodes[ERROR_ACCESS_DENIED] = XErrCode.ERROR_ACCESS_DENIED;
            sCodes[ERROR_INTERNAL_ERROR] = XErrCode.ERROR_INTERNAL_ERROR;
            sCodes[ERROR_PATH_NOT_FOUND] = XErrCode.ERROR_PATH_NOT_FOUND;
            sCodes[ERROR_FILE_NOT_FOUND] = XErrCode.ERROR_FILE_NOT_FOUND;
            sCodes[ERROR_INVALID_HANDLE] = XErrCode.ERROR_INVALID_HANDLE;
            sCodes[ERROR_WRITE_PROTECT] = XErrCode.ERROR_WRITE_PROTECT;
            sCodes[ERROR_LOCK_VIOLATION] = XErrCode.ERROR_LOCK_VIOLATION;
            sCodes[ERROR_HANDLE_EOF] = XErrCode.ERROR_HANDLE_EOF;
            sCodes[ERROR_HANDLE_DISK_FULL] = XErrCode.ERROR_HANDLE_DISK_FULL;
            sCodes[ERROR_NOT_SUPPORTED] = XErrCode.ERROR_NOT_SUPPORTED;
            sCodes[ERROR_UNEXP_NET_ERR] = XErrCode.ERROR_UNEXP_NET_ERR;
            sCodes[ERROR_INVALID_PARAMETER] = XErrCode.ERROR_INVALID_PARAMETER;
            sCodes[ERROR_INVALID_NAME] = XErrCode.ERROR_INVALID_NAME;
            sCodes[ERROR_DIR_NOT_EMPTY] = XErrCode.ERROR_DIR_NOT_EMPTY;
            sCodes[ERROR_LOCK_FAILED] = XErrCode.ERROR_LOCK_FAILED;
            sCodes[ERROR_DELETE_PENDING] = XErrCode.ERROR_DELETE_PENDING;
            sCodes[ERROR_CAN_NOT_COMPLETE] = XErrCode.ERROR_CAN_NOT_COMPLETE;
            sCodes[ERROR_NO_MEDIA_IN_DRIVE] = XErrCode.ERROR_NO_MEDIA_IN_DRIVE;
            sCodes[ERROR_TOO_MANY_LINKS] = XErrCode.ERROR_TOO_MANY_LINKS;
            sCodes[ERROR_DISK_QUOTA_EXCEEDED] = XErrCode.ERROR_DISK_QUOTA_EXCEEDED;
            sCodes[ERROR_FILE_CORRUPT] = XErrCode.ERROR_FILE_CORRUPT;
            sCodes[ERROR_BAD_PATHNAME] = XErrCode.ERROR_BAD_PATHNAME;
            //sCodes[ERROR_FILE_CHECKED_OUT] = XErrCode.ERROR_FILE_CHECKED_OUT;

            sCodes[OK] = XErrCode.ERROR_OK;
        }
    }
}
