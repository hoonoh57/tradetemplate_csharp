using System;

namespace Common.Models
{
    /// <summary>
    /// 기관/외국인 매매 데이터 — 불변(Immutable)
    /// </summary>
    public sealed class InstitutionData
    {
        public string Code { get; }
        public DateTime Date { get; }
        public long ForeignNetBuy { get; }
        public long InstitutionNetBuy { get; }
        public long InsuranceNetBuy { get; }
        public long TrustNetBuy { get; }
        public long BankNetBuy { get; }
        public long PensionNetBuy { get; }
        public long IndividualNetBuy { get; }
        public double ForeignHoldRate { get; }

        public InstitutionData(
            string code, DateTime date,
            long foreignNetBuy, long institutionNetBuy,
            long insuranceNetBuy, long trustNetBuy,
            long bankNetBuy, long pensionNetBuy,
            long individualNetBuy, double foreignHoldRate)
        {
            Code = code ?? "";
            Date = date;
            ForeignNetBuy = foreignNetBuy;
            InstitutionNetBuy = institutionNetBuy;
            InsuranceNetBuy = insuranceNetBuy;
            TrustNetBuy = trustNetBuy;
            BankNetBuy = bankNetBuy;
            PensionNetBuy = pensionNetBuy;
            IndividualNetBuy = individualNetBuy;
            ForeignHoldRate = foreignHoldRate;
        }

        public override string ToString() =>
            $"{Code} {Date:yyyy-MM-dd} Foreign:{ForeignNetBuy:+#;-#;0} Inst:{InstitutionNetBuy:+#;-#;0}";
    }
}