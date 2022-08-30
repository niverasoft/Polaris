namespace Polaris.CustomCommands
{
    public enum InstructionType
    {
        // Channel Specific
        RenameChannel,
        RemoveChannel,
        CreateChannel,
        MoveChannel,
        LockChannel,
        UnlockChannel,
        JoinVoiceChannel,
        LeaveVoiceChannel,
        EditChannel,
        ChannelInfo,

        // Member Specific
        MemberVoiceMute,
        MemberVoiceUnmute,
        MemberVoiceDeafen,
        MemberVoiceUndeafen,
        MemberVoiceKick,
        MemberInfo,

        // Role Specific
        RoleRename,
        RoleRemove,
        RoleAdd,
        RoleInfo,
        RoleAssign,
        RoleUnassign,

        // Guild Specific
        GuildChangeBanner,
        GuildChangeVoiceRegion,
        GuildChangeName,
        GuildChangeImage,
        GuildInfo,

        // Emoji Specific
        EmojiRemove,
        EmojiAdd,
        EmojiRename,
        EmojiInfo,

        // Message Specific
        MessageRespond,
        MessageSend,
        MessageDelete,
        MessageEdit,
        MessageInfo,

        // Environment Specific
        SetVariable,
        DestroyVariable,
        InfoEdit,
        InfoNull,
        
        // IO 
        IODownloadString,
        IODownloadData,
        IOUploadString,
        IOUploadData,

        // JSON
        JSONDeserialize,
        JSONSelectToken,
        JSONSelectValue,
        JSONSerialize,

        // Math
        MathAdd,
        MathSubstract,
        MathDivide,
        MathFloor,
    }
}
