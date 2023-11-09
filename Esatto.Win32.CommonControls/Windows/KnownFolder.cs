using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Windows
{
    public static class KnownFolder
    {
        // Can't use preservesig=false since ppszPath may be allocated even on failure
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SHGetKnownFolderPath(
            [MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
            KnownFolderFlag dwFlags,
            IntPtr hToken,
            out IntPtr ppszPath
        );

        public static string GetPath(Guid knownFolderID) => GetPath(knownFolderID, KnownFolderFlag.Default);
        public static string GetPath(Guid knownFolderID, KnownFolderFlag flags)
        {
            var hr = SHGetKnownFolderPath(knownFolderID, flags, IntPtr.Zero, out var path);
            try
            {
                Marshal.ThrowExceptionForHR(hr);
                return Marshal.PtrToStringUni(path);
            }
            finally
            {
                Marshal.FreeCoTaskMem(path);
            }
        }
    }

    public enum KnownFolderFlag : uint
    {
        Default = 0x00000000,
        ForceAppDataRedirection = 0x00080000,
        ReturnFilterRedirectionTarget = 0x00040000,
        ForcePackageRedirection = 0x00020000,
        NoPackageRedirection = 0x00010000,
        ForceAppContainerRedirection = 0x00020000,
        NoAppContainerRedirection = 0x00010000,
        Create = 0x00008000,
        DontVerify = 0x00004000,
        DontUnexpand = 0x00002000,
        NoAlias = 0x00001000,
        Init = 0x00000800,
        DefaultPath = 0x00000400,
        NotParentRelative = 0x00000200,
        SimpleIdList = 0x00000100,
        AliasOnly = 0x80000000
    };

    public static class KnownFolders
    {
        public static readonly Guid
            NetworkFolder = Guid.Parse("d20beec4-5ca8-4905-ae3b-bf251ea09b53"),
            ComputerFolder = Guid.Parse("0ac0837c-bbf8-452a-850d-79d08e667ca7"),
            InternetFolder = Guid.Parse("4d9f7874-4e0c-4904-967b-40b0d20c3e4b"),
            ControlPanelFolder = Guid.Parse("82a74aeb-aeb4-465c-a014-d097ee346d63"),
            PrintersFolder = Guid.Parse("76fc4e2d-d6ad-4519-a663-37bd56068185"),
            SyncManagerFolder = Guid.Parse("43668bf8-c14e-49b2-97c9-747784d784b7"),
            SyncSetupFolder = Guid.Parse("0f214138-b1d3-4a90-bba9-27cbc0c5389a"),
            ConflictFolder = Guid.Parse("4bfefb45-347d-4006-a5be-ac0cb0567192"),
            SyncResultsFolder = Guid.Parse("289a9a43-be44-4057-a41b-587a76d7e7f9"),
            RecycleBinFolder = Guid.Parse("b7534046-3ecb-4c18-be4e-64cd4cb7d6ac"),
            ConnectionsFolder = Guid.Parse("6f0cd92b-2e97-45d1-88ff-b0d186b8dedd"),
            Fonts = Guid.Parse("fd228cb7-ae11-4ae3-864c-16f3910ab8fe"),
            Desktop = Guid.Parse("b4bfcc3a-db2c-424c-b029-7fe99a87c641"),
            Startup = Guid.Parse("b97d20bb-f46a-4c97-ba10-5e3608430854"),
            Programs = Guid.Parse("a77f5d77-2e2b-44c3-a6a2-aba601054a51"),
            StartMenu = Guid.Parse("625b53c3-ab48-4ec1-ba1f-a1ef4146fc19"),
            Recent = Guid.Parse("ae50c081-ebd2-438a-8655-8a092e34987a"),
            SendTo = Guid.Parse("8983036c-27c0-404b-8f08-102d10dcfd74"),
            Documents = Guid.Parse("fdd39ad0-238f-46af-adb4-6c85480369c7"),
            Favorites = Guid.Parse("1777f761-68ad-4d8a-87bd-30b759fa33dd"),
            NetHood = Guid.Parse("c5abbf53-e17f-4121-8900-86626fc2c973"),
            PrintHood = Guid.Parse("9274bd8d-cfd1-41c3-b35e-b13f55a758f4"),
            Templates = Guid.Parse("a63293e8-664e-48db-a079-df759e0509f7"),
            CommonStartup = Guid.Parse("82a5ea35-d9cd-47c5-9629-e15d2f714e6e"),
            CommonPrograms = Guid.Parse("0139d44e-6afe-49f2-8690-3dafcae6ffb8"),
            CommonStartMenu = Guid.Parse("a4115719-d62e-491d-aa7c-e74b8be3b067"),
            PublicDesktop = Guid.Parse("c4aa340d-f20f-4863-afef-f87ef2e6ba25"),
            ProgramData = Guid.Parse("62ab5d82-fdc1-4dc3-a9dd-070d1d495d97"),
            CommonTemplates = Guid.Parse("b94237e7-57ac-4347-9151-b08c6c32d1f7"),
            PublicDocuments = Guid.Parse("ed4824af-dce4-45a8-81e2-fc7965083634"),
            RoamingAppData = Guid.Parse("3eb685db-65f9-4cf6-a03a-e3ef65729f3d"),
            LocalAppData = Guid.Parse("f1b32785-6fba-4fcf-9d55-7b8e7f157091"),
            LocalAppDataLow = Guid.Parse("a520a1a4-1780-4ff6-bd18-167343c5af16"),
            InternetCache = Guid.Parse("352481e8-33be-4251-ba85-6007caedcf9d"),
            Cookies = Guid.Parse("2b0f765d-c0e9-4171-908e-08a611b84ff6"),
            History = Guid.Parse("d9dc8a3b-b784-432e-a781-5a1130a75963"),
            System = Guid.Parse("1ac14e77-02e7-4e5d-b744-2eb1ae5198b7"),
            SystemX86 = Guid.Parse("d65231b0-b2f1-4857-a4ce-a8e7c6ea7d27"),
            Windows = Guid.Parse("f38bf404-1d43-42f2-9305-67de0b28fc23"),
            Profile = Guid.Parse("5e6c858f-0e22-4760-9afe-ea3317b67173"),
            Pictures = Guid.Parse("33e28130-4e1e-4676-835a-98395c3bc3bb"),
            ProgramFilesX86 = Guid.Parse("7c5a40ef-a0fb-4bfc-874a-c0f2e0b9fa8e"),
            ProgramFilesCommonX86 = Guid.Parse("de974d24-d9c6-4d3e-bf91-f4455120b917"),
            ProgramFilesX64 = Guid.Parse("6d809377-6af0-444b-8957-a3773f02200e"),
            ProgramFilesCommonX64 = Guid.Parse("6365d5a7-0f0d-45e5-87f6-0da56b6a4f7d"),
            ProgramFiles = Guid.Parse("905e63b6-c1bf-494e-b29c-65b732d3d21a"),
            ProgramFilesCommon = Guid.Parse("f7f1ed05-9f6d-47a2-aaae-29d317c6f066"),
            UserProgramFiles = Guid.Parse("5cd7aee2-2219-4a67-b85d-6c9ce15660cb"),
            UserProgramFilesCommon = Guid.Parse("bcbd3057-ca5c-4622-b42d-bc56db0ae516"),
            AdminTools = Guid.Parse("724ef170-a42d-4fef-9f26-b60e846fba4f"),
            CommonAdminTools = Guid.Parse("d0384e7d-bac3-4797-8f14-cba229b392b5"),
            Music = Guid.Parse("4bd8d571-6d19-48d3-be97-422220080e43"),
            Videos = Guid.Parse("18989b1d-99b5-455b-841c-ab7c74e4ddfc"),
            Ringtones = Guid.Parse("c870044b-f49e-4126-a9c3-b52a1ff411e8"),
            PublicPictures = Guid.Parse("b6ebfb86-6907-413c-9af7-4fc2abf07cc5"),
            PublicMusic = Guid.Parse("3214fab5-9757-4298-bb61-92a9deaa44ff"),
            PublicVideos = Guid.Parse("2400183a-6185-49fb-a2d8-4a392a602ba3"),
            PublicRingtones = Guid.Parse("e555ab60-153b-4d17-9f04-a5fe99fc15ec"),
            ResourceDir = Guid.Parse("8ad10c31-2adb-4296-a8f7-e4701232c972"),
            LocalizedResourcesDir = Guid.Parse("2a00375e-224c-49de-b8d1-440df7ef3ddc"),
            CommonOEMLinks = Guid.Parse("c1bae2d0-10df-4334-bedd-7aa20b227a9d"),
            CDBurning = Guid.Parse("9e52ab10-f80d-49df-acb8-4330f5687855"),
            UserProfiles = Guid.Parse("0762d272-c50a-4bb0-a382-697dcd729b80"),
            Playlists = Guid.Parse("de92c1c7-837f-4f69-a3bb-86e631204a23"),
            SamplePlaylists = Guid.Parse("15ca69b3-30ee-49c1-ace1-6b5ec372afb5"),
            SampleMusic = Guid.Parse("b250c668-f57d-4ee1-a63c-290ee7d1aa1f"),
            SamplePictures = Guid.Parse("c4900540-2379-4c75-844b-64e6faf8716b"),
            SampleVideos = Guid.Parse("859ead94-2e85-48ad-a71a-0969cb56a6cd"),
            PhotoAlbums = Guid.Parse("69d2cf90-fc33-4fb7-9a0c-ebb0f0fcb43c"),
            Public = Guid.Parse("dfdf76a2-c82a-4d63-906a-5644ac457385"),
            ChangeRemovePrograms = Guid.Parse("df7266ac-9274-4867-8d55-3bd661de872d"),
            AppUpdates = Guid.Parse("a305ce99-f527-492b-8b1a-7e76fa98d6e4"),
            AddNewPrograms = Guid.Parse("de61d971-5ebc-4f02-a3a9-6c82895e5c04"),
            Downloads = Guid.Parse("374de290-123f-4565-9164-39c4925e467b"),
            PublicDownloads = Guid.Parse("3d644c9b-1fb8-4f30-9b45-f670235f79c0"),
            SavedSearches = Guid.Parse("7d1d3a04-debb-4115-95cf-2f29da2920da"),
            QuickLaunch = Guid.Parse("52a4f021-7b75-48a9-9f6b-4b87a210bc8f"),
            Contacts = Guid.Parse("56784854-c6cb-462b-8169-88e350acb882"),
            SidebarParts = Guid.Parse("a75d362e-50fc-4fb7-ac2c-a8beaa314493"),
            SidebarDefaultParts = Guid.Parse("7b396e54-9ec5-4300-be0a-2482ebae1a26"),
            PublicGameTasks = Guid.Parse("debf2536-e1a8-4c59-b6a2-414586476aea"),
            GameTasks = Guid.Parse("054fae61-4dd8-4787-80b6-090220c4b700"),
            SavedGames = Guid.Parse("4c5c32ff-bb9d-43b0-b5b4-2d72e54eaaa4"),
            Games = Guid.Parse("cac52c1a-b53d-4edc-92d7-6b2e8ac19434"),
            SEARCH_MAPI = Guid.Parse("98ec0e18-2098-4d44-8644-66979315a281"),
            SEARCH_CSC = Guid.Parse("ee32e446-31ca-4aba-814f-a5ebd2fd6d5e"),
            Links = Guid.Parse("bfb9d5e0-c6a9-404c-b2b2-ae6db6af4968"),
            UsersFiles = Guid.Parse("f3ce0f7c-4901-4acc-8648-d5d44b04ef8f"),
            UsersLibraries = Guid.Parse("a302545d-deff-464b-abe8-61c8648d939b"),
            SearchHome = Guid.Parse("190337d1-b8ca-4121-a639-6d472d16972a"),
            OriginalImages = Guid.Parse("2c36c0aa-5812-4b87-bfd0-4cd0dfb19b39"),
            DocumentsLibrary = Guid.Parse("7b0db17d-9cd2-4a93-9733-46cc89022e7c"),
            MusicLibrary = Guid.Parse("2112ab0a-c86a-4ffe-a368-0de96e47012e"),
            PicturesLibrary = Guid.Parse("a990ae9f-a03b-4e80-94bc-9912d7504104"),
            VideosLibrary = Guid.Parse("491e922f-5643-4af4-a7eb-4e7a138d8174"),
            RecordedTVLibrary = Guid.Parse("1a6fdba2-f42d-4358-a798-b74d745926c5"),
            HomeGroup = Guid.Parse("52528a6b-b9e3-4add-b60d-588c2dba842d"),
            HomeGroupCurrentUser = Guid.Parse("9b74b6a3-0dfd-4f11-9e78-5f7800f2e772"),
            DeviceMetadataStore = Guid.Parse("5ce4a5e9-e4eb-479d-b89f-130c02886155"),
            Libraries = Guid.Parse("1b3ea5dc-b587-4786-b4ef-bd1dc332aeae"),
            PublicLibraries = Guid.Parse("48daf80b-e6cf-4f4e-b800-0e69d84ee384"),
            UserPinned = Guid.Parse("9e3995ab-1f9c-4f13-b827-48b24b6c7174"),
            ImplicitAppShortcuts = Guid.Parse("bcb5256f-79f6-4cee-b725-dc34e402fd46"),
            AccountPictures = Guid.Parse("008ca0b1-55b4-4c56-b8a8-4de4b299d3be"),
            PublicUserTiles = Guid.Parse("0482af6c-08f1-4c34-8c90-e17ec98b1e17"),
            AppsFolder = Guid.Parse("1e87508d-89c2-42f0-8a7e-645a0f50ca58"),
            StartMenuAllPrograms = Guid.Parse("f26305ef-6948-40b9-b255-81453d09c785"),
            CommonStartMenuPlaces = Guid.Parse("a440879f-87a0-4f7d-b700-0207b966194a"),
            ApplicationShortcuts = Guid.Parse("a3918781-e5f2-4890-b3d9-a7e54332328c"),
            RoamingTiles = Guid.Parse("00bcfc5a-ed94-4e48-96a1-3f6217f21990"),
            RoamedTileImages = Guid.Parse("aaa8d5a5-f1d6-4259-baa8-78e7ef60835e"),
            Screenshots = Guid.Parse("b7bede81-df94-4682-a7d8-57a52620b86f"),
            CameraRoll = Guid.Parse("ab5fb87b-7ce2-4f83-915d-550846c9537b"),
            SkyDrive = Guid.Parse("a52bba46-e9e1-435f-b3d9-28daa648c0f6"),
            OneDrive = Guid.Parse("a52bba46-e9e1-435f-b3d9-28daa648c0f6"),
            SkyDriveDocuments = Guid.Parse("24d89e24-2f19-4534-9dde-6a6671fbb8fe"),
            SkyDrivePictures = Guid.Parse("339719b5-8c47-4894-94c2-d8f77add44a6"),
            SkyDriveMusic = Guid.Parse("c3f2459e-80d6-45dc-bfef-1f769f2be730"),
            SkyDriveCameraRoll = Guid.Parse("767e6811-49cb-4273-87c2-20f355e1085b"),
            SearchHistory = Guid.Parse("0d4c3db6-03a3-462f-a0e6-08924c41b5d4"),
            SearchTemplates = Guid.Parse("7e636bfe-dfa9-4d5e-b456-d7b39851d8a9"),
            CameraRollLibrary = Guid.Parse("2b20df75-1eda-4039-8097-38798227d5b7"),
            SavedPictures = Guid.Parse("3b193882-d3ad-4eab-965a-69829d1fb59f"),
            SavedPicturesLibrary = Guid.Parse("e25b5812-be88-4bd9-94b0-29233477b6c3"),
            RetailDemo = Guid.Parse("12d4c69e-24ad-4923-be19-31321c43a767"),
            Device = Guid.Parse("1c2ac1dc-4358-4b6c-9733-af21156576f0"),
            DevelopmentFiles = Guid.Parse("dbe8e08e-3053-4bbc-b183-2a7b2b191e59"),
            Objects3D = Guid.Parse("31c0dd25-9439-4f12-bf41-7ff4eda38722"),
            AppCaptures = Guid.Parse("edc0fe71-98d8-4f4a-b920-c8dc133cb165"),
            LocalDocuments = Guid.Parse("f42ee2d3-909f-4907-8871-4c22fc0bf756"),
            LocalPictures = Guid.Parse("0ddd015d-b06c-45d5-8c4c-f59713854639"),
            LocalVideos = Guid.Parse("35286a68-3c57-41a1-bbb1-0eae73d76c95"),
            LocalMusic = Guid.Parse("a0c69a99-21c8-4671-8703-7934162fcf1d"),
            LocalDownloads = Guid.Parse("7d83ee9b-2244-4e70-b1f5-5393042af1e4"),
            RecordedCalls = Guid.Parse("2f8b40c2-83ed-48ee-b383-a1f157ec6f9a"),
            AllAppMods = Guid.Parse("7ad67899-66af-43ba-9156-6aad42e6c596"),
            CurrentAppMods = Guid.Parse("3db40b20-2a30-4dbe-917e-771dd21dd099"),
            AppDataDesktop = Guid.Parse("b2c5e279-7add-439f-b28c-c41fe1bbf672"),
            AppDataDocuments = Guid.Parse("7be16610-1f7f-44ac-bff0-83e15f2ffca1"),
            AppDataFavorites = Guid.Parse("7cfbefbc-de1f-45aa-b843-a542ac536cc9"),
            AppDataProgramData = Guid.Parse("559d40a3-a036-40fa-af61-84cb430a4d34"),
            LocalStorage = Guid.Parse("b3eb08d3-a1f3-496b-865a-42b536cda0ec");
    }
}
