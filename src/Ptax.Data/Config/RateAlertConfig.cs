using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ptax.Data.Config;

public class RateAlertConfig : IEntityTypeConfiguration<RateAlert>
{
    public void Configure(EntityTypeBuilder<RateAlert> builder)
    {
        builder.ToTable("rate_alerts");

        builder.HasKey(alert => new { alert.Currency, alert.Date });

        builder.Property(p => p.Currency)
            .HasMaxLength(3)
            .HasColumnName("currency");

        builder.Property(p => p.PreviousDate)
            .HasColumnName("previous_date");

        builder.Property(p => p.Date)
            .HasColumnName("date");

        builder.Property(p => p.PreviousSellRate)
            .HasPrecision(18, 5)
            .HasColumnName("previous_sell_rate");

        builder.Property(p => p.SellRate)
            .HasPrecision(18, 5)
            .HasColumnName("sell_rate");

        builder.Property(p => p.ChangePercent)
            .HasPrecision(9, 2)
            .HasColumnName("change_percent");
    }
}