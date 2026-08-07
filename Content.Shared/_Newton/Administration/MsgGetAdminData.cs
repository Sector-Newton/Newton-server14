using Content.Shared.Administration;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Newton.Administration
{
    public sealed class MsgUpdateAdminData : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public AdminData? Admin;
        public NetEntity TargetUserId;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {

            if (buffer.ReadBoolean())
            {
                var active = buffer.ReadBoolean();
                var stealth = buffer.ReadBoolean();
                TargetUserId = new NetEntity(buffer.ReadInt32());
                buffer.ReadPadBits();

                Admin = new AdminData
                {
                    Active = active,
                    Stealth = stealth,
                };
            }

        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(Admin != null);

            if (Admin == null) return;

            buffer.Write(Admin.Active);
            buffer.Write(Admin.Stealth);
            buffer.Write(TargetUserId.Id);
            buffer.WritePadBits();
        }

        public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;
    }
}
