using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAppointmentManagement.Infrastructure.Migrations
{
    public partial class FN_GetUpcomingAppoinmentsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
create function [dbo].[FN_GetUpcomingAppoinments]
as
return(
select * from dbo.DoctorSlot
join AppoimentBooking
on slot.Id =AppoimentBooking.SlotId dbo. where Date>=GetDate())
");
        }
    }
}
