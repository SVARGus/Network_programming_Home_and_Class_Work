using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rock_Paper_Scissors_UDPClient
{
    public enum MoveType
    {
        Rock,
        Paper,
        Scissors
    }

    public enum MatchOutcome
    {
        Draw,
        Player1Win,
        Player2Win
    }

    public enum PacketType
    {
        SettingsProposal,
        SettingsAck,
        MoveSubmission,
        MoveAck,
        GameResult,
        MatchResult
    }

    public class UdpPacket<T>
    {
        public PacketType Type {  get; set; }
        //public Guid MarchId { get; set; }
        public int GameNamber {  get; set; }
        public int RoundNumber {  get; set; }
        public T Payload { get; set; }
    }

    public class SettingsPayload
    {
        public int TotalGame { get; set; }
        public int MatchPerGame { get; set; }
    }

    public class SettingsAckPayload
    {
        public bool Accepted { get; set; }
    }

    public class MovePayload
    {
        public MoveType? Move {  get; set; }
        public bool OfferDraw { get; set; }
        public bool Resing { get; set; }
    }

    public class TotalResult
    {
        public List<GameResultPayload> Results { get; set; }
        public MatchOutcome Outcome { get; set; }
        public MoveType MostPopularMove { get; set; }
        public MoveType LeastPopularMove { get; set; }
    }

    public class GameResultPayload
    {
        public List<MatchResultPayload> Match {  get; set; }
        public MatchOutcome Outcome { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class MatchResultPayload
    {
        public MoveType PlayerMove { get; set; }
        public MoveType OpponentMove { get; set; }
        public MatchOutcome Outcome { get; set; }
    }
}
