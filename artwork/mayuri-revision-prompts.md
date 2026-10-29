# 真由理手部与短袖修订 · 2026-10-29

## 右肘局部修正（0.4.3）

去除画面左下方、真由理右肘下残留的浅蓝色独立衣料。使用内置 image_gen，输入当前睡姿全图及右肘局部定位图，要求保留透明背景。生成结果仅取 1341×1173 原画布中 (304,994,152,106) 的局部，以 8 像素边缘过渡合入 source/Mayuri-sleep.png；核对修补范围外 RGBA 像素完全一致。局部生成素材另存 source/Mayuri-sleep-elbow-patch.png。正式睡姿仍导出到 ../assets/characters/Mayuri-sleep.png。

```text
Use case: precise-object-edit. Image 1 is the exact full transparent sprite edit target; image 2 is a magnified detail for locating the defect, NOT an additional object or output. Return the full sprite with the identical composition and canvas aspect ratio as Image 1. Make ONE TINY CORRECTION ONLY: at the extreme viewer-left bottom, underneath the character's right bare elbow, a pale blue triangular cloth wedge is sticking out behind and below the peach skin of the elbow. This is an erroneous leftover detached sleeve, not part of the dress. REMOVE that pale blue wedge entirely, replacing it with genuinely transparent background. Keep the existing rounded peach elbow skin, its outline and all skin shading exactly as they are. Restore only a clean continuous dark contour where necessary along the outside of that elbow. The light blue puff short sleeve above the dark sleeve trim is correct and MUST stay. Preserve every other part unchanged: both folded hands, all fingers, forearms, opposite elbow, upper sleeves, lace neckline, face, closed eyes, smile, hat, ribbon, hair, colours, shading, scale, pose and transparent margins. Do not enlarge or redraw the elbow or the whole character. No additional objects, no text, no shadows. Genuine alpha transparency.
```

使用内置 image_gen 编辑，要求真实透明背景。生成原图保存在 source/Mayuri-hands.png 与 source/Mayuri-sleep.png；通过 prepare-art.ps1 -Character Mayuri 对齐、分层和导出正式 PNG。运行时无需生成模型或 Node.js。

原作对照：https://steinsgate.jp/darling/common/img/cc2.png （官方《比翼恋理的爱人》角色立绘）。短泡泡袖之后是裸露前臂，初版桌宠增加的独立蓝色喇叭袖口不符合这张立绘。待机只接入下方手部区，保留基准头部和身体；睡姿整体导出。

## 待机手部（precise-object-edit）

输入：修订前 assets/characters/Mayuri.png，作为精确编辑目标和画风参考。

```text
Use case: precise-object-edit. Asset: transparent Mayuri keyboard-hands sprite source for Lab Pals. Supplied image is the exact edit target and approved character. Keep EVERYTHING above the dark trimmed short puff sleeves exactly unchanged: hat, bow, hair, face, eyes, head position, shoulders, torso, pink lace, waist bow, palette, linework, cel shading, scale, canvas composition. Correct only the lower forearms and hands: Mayuri wears pale blue puff SHORT sleeves ending in dark trim near the elbows, with bare forearms naturally continuing to the hands. REMOVE the incorrect separate blue bell-shaped detached cuffs flaring out to both sides of the hands at the very bottom. In their place show short anatomically continuous bare forearms and natural wrists. Keep both relaxed small human hands palms down in the SAME original keyboard-typing positions, left hand on viewer left and right hand on viewer right, fingertips pointed toward the bottom, all fingers clearly separated with plausible thumbs, preserve hand scale and fingertip baseline. Character still ends just below the fingertips. No long sleeves, no wrist bands, no detached cloth, no gloves, no added objects or keyboard. Preserve the waist bow behind the hands and the complete transparent margin. Single waist-up chibi character, no text, no effects, genuinely transparent alpha background.
```

## 睡姿（precise-object-edit）

输入 1：修订前 source/Mayuri-sleep.png，编辑目标。输入 2：修订前 assets/characters/Mayuri.png，角色与画风参考。旧图可从 Git 中恢复。

```text
Use case: precise-object-edit. Asset: replacement transparent PNG sleeping sprite for the Lab Pals Windows desktop companion. Image 1 is the edit target sleeping Mayuri; Image 2 is the approved awake character identity/style reference. Preserve Image 1's exact face, closed eyes, expression, head tilt, short charcoal hair, pale blue soft hat and blue bow, head position and scale, delicate dark outlines and restrained cel shading. REDRAW ONLY THE LOWER ARMS AND HANDS with clear correct human anatomy. The current crossed hands have ambiguous reversed wrist connections. Replace them with two relaxed small human hands resting naturally one over the other under her cheek, palms down, with clean distinct fingers and plausible thumbs. Each hand must connect continuously to its own wrist and forearm coming from the corresponding shoulder; avoid twisted wrists, extra fingers, fused hands, disconnected cuffs or duplicate skin patches. Simplify the overlap so the pose reads perfectly at 176 pixels wide. Costume has pale blue puff SHORT sleeves with dark trim, a short exposed forearm, and NO separate blue detached bell sleeves or floating cuffs around the wrists. Preserve pink lace neckline. Crop ends just below the folded hands. No furniture, no keyboard, no objects, no text, no symbols or ZZZ, no shadows outside character. Keep the same overall silhouette, composition and transparent surrounding canvas as Image 1. Genuine transparent alpha background.
```
