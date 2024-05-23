using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace RemoteNodeWindowFirewallApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class WindowFirewallController : ControllerBase
    {
        private readonly ILogger<WindowFirewallController> _logger;

        public WindowFirewallController(ILogger<WindowFirewallController> logger)
        {
            _logger = logger;
        }

        // GET api/WindowFirewall/exists/ruleName
        [HttpGet("exists/{ruleName}")]
        public ActionResult<bool> RuleExists(string ruleName)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall show rule name={ruleName}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return !output.Contains("No rules match the specified criteria");
        }

        // GET api/WindowFirewall/remoteips/ruleName
        [HttpGet("remoteips/{ruleName}")]
        public ActionResult<IEnumerable<string>> GetRemoteIPs(string ruleName)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall show rule name={ruleName} verbose",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (output.Contains("No rules match the specified criteria"))
            {
                return NotFound("Rule not found");
            }

            var lines = output.Split('\n');
            var remoteIPLine = lines.FirstOrDefault(line => line.StartsWith("RemoteIP:"));
            if (remoteIPLine == null)
            {
                return NotFound("No remote IPs found for the rule");
            }

            var ips = remoteIPLine.Substring(10).Split(',').Select(ip => ip.Trim()).ToList();
            return Ok(ips);
        }

        // Add api/WindowFirewall/remoteips/ruleName
        [HttpPost("remoteips/{ruleName}/add")]
        public IActionResult AddIP(string ruleName, string ipAddress)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall set rule name={ruleName} new remoteip={ipAddress}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (output.Contains("No rules match the specified criteria"))
            {
                return NotFound("Rule not found");
            }

            if (output.Contains("Ok."))
            {
                return Ok();
            }

            return BadRequest("Failed to add IP");
        }

        // DELETE api/WindowFirewall/removeip/ruleName/ipAddress
        [HttpDelete("remoteips/{ruleName}/{ipAddress}")]
        public IActionResult RemoveIP(string ruleName, string ipAddress)
        {
            // Get existing IPs
            var existingIPs = GetRemoteIPs(ruleName);
            if (!(existingIPs?.Value?.Any() == true))
            {
                return NotFound("No existing IPs found for the rule");
            }

            // Remove the specified IP
            var updatedIPs = existingIPs.Value.Where(ip => ip != ipAddress).ToList();
            if (updatedIPs.Count == existingIPs.Value.Count())
            {
                return NotFound("IP not found in the rule");
            }

            // Update the rule with the new list of IPs
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall set rule name={ruleName} new remoteip={string.Join(",", updatedIPs)}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (output.Contains("No rules match the specified criteria"))
            {
                return NotFound("Rule not found");
            }

            if (output.Contains("Ok."))
            {
                return Ok();
            }

            return BadRequest("Failed to remove IP");
        }

        // DELETE api/WindowFirewall/clearips/ruleName
        [HttpDelete("clearips/{ruleName}")]
        public IActionResult ClearIPs(string ruleName)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall set rule name={ruleName} new remoteip=any",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (output.Contains("No rules match the specified criteria"))
            {
                return NotFound("Rule not found");
            }

            if (output.Contains("Ok."))
            {
                return Ok();
            }

            return BadRequest("Failed to clear IPs");
        }

    }
}
