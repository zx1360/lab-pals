// Offline registration, sprite segmentation and atlas preparation. No runtime dependency.
const fs = require('node:fs');
const path = require('node:path');
const sharp = require(process.argv[2] || 'sharp');
const root=path.resolve(__dirname,'../..'), art=path.join(root,'artwork'), out=path.join(root,'assets/characters');
const catalogPath=path.join(root,'assets/characters.json'),catalog=JSON.parse(fs.readFileSync(catalogPath,'utf8'));
const names=process.argv[3]?[process.argv[3]]:catalog.characters.map(character=>character.id);
for(const name of names)if(!catalog.characters.some(character=>character.id===name))throw new Error('Unknown character: '+name);
const W=640,H=560;
async function raw(file){return sharp(file).resize(W,H,{fit:'fill'}).ensureAlpha().raw().toBuffer();}
async function save(data,file){await sharp(data,{raw:{width:W,height:H,channels:4}}).png({compressionLevel:9}).toFile(file);}
function bounds(data){let l=W,t=H,r=0,b=0;for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(data[(y*W+x)*4+3]>64){l=Math.min(l,x);r=Math.max(r,x);t=Math.min(t,y);b=Math.max(b,y);}return {left:l,top:t,width:r-l+1,height:b-t+1};}
function sample(data,x,y,ch){x=Math.round(x);y=Math.round(y);return x>=0&&y>=0&&x<W&&y<H?data[(y*W+x)*4+ch]:0;}
function register(original,edited){
  // Solve a small similarity transform against unchanged hair/hat/upper costume, excluding the face.
  const points=[];
  for(let y=25;y<440;y+=8)for(let x=95;x<550;x+=8){
    if(x>215&&x<420&&y>170&&y<335)continue;
    if(original[(y*W+x)*4+3]>220)points.push([x,y]);
  }
  function score(s,dx,dy){let sum=0;for(const[x,y]of points){
    const xx=(x-320)/s+320-dx,yy=(y-280)/s+280-dy,i=(y*W+x)*4;
    for(let c=0;c<4;c++){const d=original[i+c]-sample(edited,xx,yy,c);sum+=d*d;}
  }return sum/points.length;}
  let best={score:Infinity,s:1,dx:0,dy:0};
  for(let si=96;si<=104;si++)for(let dx=-8;dx<=8;dx+=2)for(let dy=-8;dy<=8;dy+=2){const s=si/100,v=score(s,dx,dy);if(v<best.score)best={score:v,s,dx,dy};}
  let refined=best;
  for(let si=-4;si<=4;si++)for(let dx=-1.5;dx<=1.5;dx+=.5)for(let dy=-1.5;dy<=1.5;dy+=.5){
    const s=best.s+si*.002,v=score(s,best.dx+dx,best.dy+dy);if(v<refined.score)refined={score:v,s,dx:best.dx+dx,dy:best.dy+dy};
  }
  const result=Buffer.alloc(W*H*4),{s,dx,dy}=refined;
  for(let y=0;y<H;y++)for(let x=0;x<W;x++){
    const xx=(x-320)/s+320-dx,yy=(y-280)/s+280-dy,ix=Math.floor(xx),iy=Math.floor(yy),fx=xx-ix,fy=yy-iy;
    for(let c=0;c<4;c++)result[(y*W+x)*4+c]=Math.round(sample(edited,ix,iy,c)*(1-fx)*(1-fy)+sample(edited,ix+1,iy,c)*fx*(1-fy)+sample(edited,ix,iy+1,c)*(1-fx)*fy+sample(edited,ix+1,iy+1,c)*fx*fy);
  }
  return {data:result,transform:refined};
}
function handMask(data,side){
  const minX=side===0?145:325,maxX=side===0?315:500,minY=447,maxY=552;
  const seen=new Uint8Array(W*H);let largest=[];const components=[];
  function skin(i){const r=data[i*4],g=data[i*4+1],b=data[i*4+2];return data[i*4+3]>80&&r>200&&g>150&&r-g>10&&r-b>19&&g-b>6;}
  for(let y=minY;y<maxY;y++)for(let x=minX;x<maxX;x++){
    const first=y*W+x;if(seen[first]||!skin(first))continue;
    const queue=[first];seen[first]=1;
    for(let n=0;n<queue.length;n++){const i=queue[n],xx=i%W,yy=Math.floor(i/W);for(const[j,nx,ny]of[[i-1,xx-1,yy],[i+1,xx+1,yy],[i-W,xx,yy-1],[i+W,xx,yy+1]]){
      if(nx>=minX&&nx<maxX&&ny>=minY&&ny<maxY&&!seen[j]&&skin(j)){seen[j]=1;queue.push(j);}
    }}
    components.push(queue);if(queue.length>largest.length)largest=queue;
  }
  if(largest.length<700)throw new Error('Human hand segmentation too small: '+side+' '+largest.length);
  const mask=new Uint8Array(W*H);let top=H;
  for(const i of largest)top=Math.min(top,Math.floor(i/W));
  const meanX=largest.reduce((sum,i)=>sum+i%W,0)/largest.length,near=new Uint8Array(W*H);
  for(const i of largest){const x=i%W,y=Math.floor(i/W);for(let dy=-7;dy<=7;dy++)for(let dx=-7;dx<=7;dx++)if(x+dx>=0&&x+dx<W&&y+dy>=0&&y+dy<H)near[(y+dy)*W+x+dx]=1;}
  const selected=largest.slice();
  for(const component of components){
    if(component===largest||component.length<15||component.length>1500)continue;
    const cx=component.reduce((sum,i)=>sum+i%W,0)/component.length,cy=component.reduce((sum,i)=>sum+Math.floor(i/W),0)/component.length;
    if(cy>top+10&&(side===0?cx>meanX:cx<meanX)&&component.some(i=>near[i]))selected.push(...component);
  }
  let px=0,count=0;
  for(const i of selected){const x=i%W,y=Math.floor(i/W);if(y<top+7){px+=x;count++;}
    for(let dy=-2;dy<=2;dy++)for(let dx=-2;dx<=2;dx++)if(dx*dx+dy*dy<=5&&x+dx>=minX&&x+dx<maxX&&y+dy>=minY&&y+dy<maxY)mask[(y+dy)*W+x+dx]=1;
  }
  return {mask,pivot:[Math.round(px/count),top+3],skinPixels:largest.length};
}
async function main(){
  fs.mkdirSync(path.join(art,'assembled'),{recursive:true});fs.mkdirSync(out,{recursive:true});
  const manifestPath=path.join(art,'rig-manifest.json');
  const manifest=fs.existsSync(manifestPath)?JSON.parse(fs.readFileSync(manifestPath,'utf8')):{};
  for(const name of names){
    const original=await raw(path.join(art,'reference',name+'.png')),closed=await raw(path.join(art,'reference',name+'-rest.png'));
    const hands=register(original,await raw(path.join(art,'source',name+'-hands.png')));
    const assembled=Buffer.from(original);
    for(let y=441;y<H;y++)for(let x=0;x<W;x++){
      const f=Math.min(1,(y-441)/15),i=(y*W+x)*4;
      for(let c=0;c<4;c++)assembled[i+c]=Math.round(original[i+c]*(1-f)+hands.data[i+c]*f);
    }
    const masks=[handMask(assembled,0),handMask(assembled,1)];
    const blink=Buffer.from(assembled);
    for(let i=0;i<W*H;i++){
      if(Math.floor(i/W)<350)for(let c=0;c<4;c++)blink[i*4+c]=closed[i*4+c];
    }
    await save(assembled,path.join(art,'assembled',name+'.png'));
    await save(assembled,path.join(out,name+'.png'));await save(blink,path.join(out,name+'-rest.png'));
    for(let side=0;side<2;side++){
      const layer=Buffer.alloc(W*H*4);
      for(let i=0;i<W*H;i++)if(masks[side].mask[i]){
        // The connected skin mask already selects the hand. Preserve all its
        // highlights and antialiased contours; a second colour filter leaves
        // fixed speckles behind when pale fingers move away from the body.
        if(assembled[i*4+3]>0)assembled.copy(layer,i*4,i*4,i*4+4);
      }
      await save(layer,path.join(out,name+(side===0?'-left.png':'-right.png')));
    }
    const reaction=register(original,await raw(path.join(art,'source',name+'-reaction.png')));
    const face=Buffer.alloc(W*H*4);
    for(let y=160;y<335;y++)for(let x=212;x<422;x++){
      const d=Math.sqrt(((x-317)/103)**2+((y-252)/82)**2),mask=Math.max(0,Math.min(1,(1-d)/.12)),i=(y*W+x)*4;
      if(mask>0){for(let c=0;c<3;c++)face[i+c]=reaction.data[i+c];face[i+3]=Math.round(original[i+3]*mask);}
    }
    await save(face,path.join(out,name+'-reaction.png'));
    const sleep=await raw(path.join(art,'source',name+'-sleep.png')),sb=bounds(sleep),ob=bounds(original);
    const sleepScale=Math.min(ob.width/sb.width,ob.height/sb.height),sw=Math.round(sb.width*sleepScale),sh=Math.round(sb.height*sleepScale);
    const asleep=await sharp(sleep,{raw:{width:W,height:H,channels:4}}).extract(sb).resize(sw,sh).png().toBuffer();
    await sharp({create:{width:W,height:H,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([{input:asleep,left:Math.round((W-sw)/2),top:ob.top+ob.height-sh}]).png({compressionLevel:9}).toFile(path.join(out,name+'-sleep.png'));
    const pivots=masks.map(h=>h.pivot.map(v=>v/2)),character=catalog.characters.find(character=>character.id===name);
    character.leftWrist=pivots[0];character.rightWrist=pivots[1];
    manifest[name]={handsRegistration:hands.transform,reactionRegistration:reaction.transform,sleepSize:[sw,sh],hands:masks.map(h=>({pivot:h.pivot,skinPixels:h.skinPixels}))};
  }
  fs.writeFileSync(catalogPath+'.tmp',JSON.stringify(catalog,null,2)+'\n');fs.renameSync(catalogPath+'.tmp',catalogPath);
  fs.writeFileSync(manifestPath,JSON.stringify(manifest,null,2)+'\n');console.log(JSON.stringify(manifest));
}
main().catch(e=>{console.error(e);process.exitCode=1;});
