using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportZone.API.Extensions;
using SportZone.Application.Dtos;
using SportZone.Application.Dtos.SportZone.Application.Dtos;
using SportZone.Application.Interfaces.IService;

namespace SportZone.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   

    public class MembersController(IMembersService memberService) : ControllerBase
    {

         [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetAlMlembers()
        {
            var members = await memberService.GetAllAsync();
            return Ok(members);
        }

         [Authorize(Roles = "Admin")]
        [HttpPost("detail")]
        public async Task<ActionResult<MemberDto>> GetMemberById([FromBody] MemberIdRequest request)
        {
            // Implement logic to retrieve a member by ID
            var member = await memberService.GetMemberDetailByIdAsync(request.MemberId);
            return Ok(member);
        }
         [Authorize(Roles = "Admin")]
        [HttpPost("add")]
        public async Task<ActionResult> AddMember(UpdateProfileDto memberDto)
        {
            // Implement logic to add a new member
            await memberService.AddAsync(memberDto);
            return Ok();
        }
         [Authorize(Roles = "Admin")]
        [HttpPut("{memberId:int}/update")]
        public async Task<ActionResult> UpdateMember(string memberId, UpdateProfileDto memberDto)
        {
            // Implement logic to update an existing member
            await memberService.UpdateAsync(memberId, memberDto);
            return Ok();
        }
         [Authorize(Roles = "Admin")]
        [HttpDelete("{memberId:int}/delete")]
        public async Task<ActionResult> DeleteMember([FromBody] string memberId)
        {
            // Implement logic to delete a member
            await memberService.DeleteAsync(memberId);
            return NoContent();
        }

        [HttpPut("redeem-reward")]
        public async Task<ActionResult> RedeemReward([FromBody] int pointsToSubtract)
        {
            var userId = User.GetUserId();

            
            await memberService.SubtractPointsAsync(userId, pointsToSubtract);

            return Ok(new { message = "Đổi quà thành công!" });
        }

    }
}
