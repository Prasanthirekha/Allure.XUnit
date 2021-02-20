using System.IO;
using System.Text;
using System.Threading.Tasks;
using Allure.Xunit;
using HeyRed.Mime;

namespace Allure.XUnit
{
    public class AllureAttachments
    {
        public static async Task Text(string name, string content) =>
            await Bytes(name, Encoding.UTF8.GetBytes(content), ".txt");

        public static async Task Bytes(string name, byte[] content, string extension) =>
            await AllureXunitHelper.AddAttachment(name, MimeTypesMap.GetMimeType(extension), content, extension);

        public static Task File(string fileName)
        {
            return File(fileName, fileName);
        }

        public static async Task File(string attachmentName, string fileName)
        {
            var content = await System.IO.File.ReadAllBytesAsync(fileName);
            var extension = Path.GetExtension(fileName);
            await Bytes(attachmentName, content, extension);
        }
    }
}