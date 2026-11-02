using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ptax.Data.Config;

public class DailyRateConfig : IEntityTypeConfiguration<DailyRate>
{
    public void Configure(EntityTypeBuilder<DailyRate> builder)
    {
        builder.ToTable("daily_rates");

        builder.HasKey(rate => new { rate.Currency, rate.Date });

        builder.Property(p => p.Currency)
            .HasMaxLength(3)
            .HasColumnName("currency");

        builder.Property(p => p.Date)
            .HasColumnName("date");

        builder.Property(p => p.BuyRate)
            .HasPrecision(18, 5)
            .HasColumnName("buy_rate");

        builder.Property(p => p.SellRate)
            .HasPrecision(18, 5)
            .HasColumnName("sell_rate");

        builder.Property(p => p.BuyParity)
            .HasPrecision(18, 5)
            .HasColumnName("buy_parity");

        builder.Property(p => p.SellParity)
            .HasPrecision(18, 5)
            .HasColumnName("sell_parity");
    }
}