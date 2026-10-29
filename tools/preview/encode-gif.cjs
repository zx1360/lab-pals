// Optional offline preview encoding. The desktop pet does not use Node.js or GIF assets.
const fs = require('node:fs');
const path = require('node:path');
const sharp = require(process.argv[4] || 'sharp');
async function main() {
  const files = fs.readdirSync(process.argv[2]).filter(s => s.endsWith('.png')).sort();
  const buffers = [];
  let width, height;
  for (const file of files) {
    const { data, info } = await sharp(path.join(process.argv[2], file)).removeAlpha().raw().toBuffer({ resolveWithObject: true });
    if (width && (info.width !== width || info.height !== height)) throw new Error('Mismatched frame dimensions');
    width = info.width; height = info.height; buffers.push(data);
  }
  const info = await sharp(Buffer.concat(buffers), { raw: { width, height: height * files.length, channels: 3, pageHeight: height } })
    .gif({ loop: 0, delay: files.map(() => 20), colours: 128, effort: 7, dither: 0, interFrameMaxError: 0, interPaletteMaxError: 3 })
    .toFile(process.argv[3]);
  console.log(JSON.stringify(info));
}
main().catch(error => { console.error(error); process.exitCode = 1; });
