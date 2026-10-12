(() => {
 const root=document.documentElement;
 const read=(key,fallback)=>{try{return localStorage.getItem(key)||fallback;}catch{return fallback;}};
 const write=(key,value)=>{try{localStorage.setItem(key,value);}catch{}};
 const theme=document.querySelector('.theme-toggle');
 const setTheme=value=>{root.dataset.theme=value;theme?.setAttribute('aria-pressed',String(value==='dark'));};
 setTheme(read('reading-theme','light'));
 theme?.addEventListener('click',()=>{setTheme(root.dataset.theme==='dark'?'light':'dark');write('reading-theme',root.dataset.theme);});
 const menu=document.querySelector('.menu-toggle'), nav=document.querySelector('#main-nav');
 const closeMenu=()=>{nav?.classList.remove('open');menu?.setAttribute('aria-expanded','false');};
 menu?.addEventListener('click',()=>{const open=nav.classList.toggle('open');menu.setAttribute('aria-expanded',String(open));});
 document.addEventListener('click',e=>{if(!e.target.closest('.header-inner'))closeMenu();document.querySelectorAll('.account-menu[open]').forEach(d=>{if(!d.contains(e.target))d.open=false;});});
 document.addEventListener('keydown',e=>{if(e.key==='Escape'){closeMenu();document.querySelectorAll('.account-menu[open]').forEach(d=>d.open=false);}});
 const slides=[...document.querySelectorAll('.hero-slide')],dots=[...document.querySelectorAll('[data-slide]')];
 dots.forEach((dot,i)=>dot.addEventListener('click',()=>{slides.forEach((s,n)=>s.hidden=n!==i);dots.forEach((d,n)=>d.setAttribute('aria-pressed',String(n===i)));}));
 let size=Math.max(16,Math.min(28,Number(read('reading-size','20'))||20));
 const setSize=()=>root.style.setProperty('--reading-size',`${size}px`);setSize();
 document.querySelectorAll('[data-font]').forEach(b=>b.addEventListener('click',()=>{size=Math.max(16,Math.min(28,size+Number(b.dataset.font)));setSize();write('reading-size',String(size));}));
 document.querySelectorAll('form[method="post"]').forEach(form=>form.addEventListener('submit',e=>{
  if(form.dataset.submitting){e.preventDefault();return;}
  form.dataset.submitting='true';
  // Keep named submit buttons enabled so the selected moderation action is submitted.
  form.querySelectorAll('button').forEach(b=>{b.setAttribute('aria-disabled','true');});
 }));
 window.addEventListener('pageshow',()=>document.querySelectorAll('form[data-submitting]').forEach(f=>{delete f.dataset.submitting;f.querySelectorAll('[aria-disabled]').forEach(b=>b.removeAttribute('aria-disabled'));}));
})();

document.querySelectorAll('.comments').forEach(section=>{
 const form=section.querySelector('.comment-form');
 section.querySelectorAll('[data-reply-id]').forEach(button=>button.addEventListener('click',()=>{
  form.elements.parentId.value=button.dataset.replyId;
  form.querySelector('.reply-target').hidden=false;
  form.querySelector('.reply-target span').textContent='Trả lời @'+button.dataset.replyAuthor;
  form.elements.text.focus();
 }));
 form?.querySelector('.cancel-reply').addEventListener('click',()=>{form.elements.parentId.value='';form.querySelector('.reply-target').hidden=true;});
});
function measureComments(){document.querySelectorAll('.comment-text').forEach(text=>{
 const button=text.parentElement.querySelector('.comment-expand');
 const expanded=button.getAttribute('aria-expanded')==='true';
 text.classList.remove('collapsed');
 const long=text.scrollHeight>parseFloat(getComputedStyle(text).lineHeight)*6+2;
 button.hidden=!long;text.classList.toggle('collapsed',long&&!expanded);
});}
document.querySelectorAll('.comment-expand').forEach(button=>button.addEventListener('click',()=>{
 const expanded=button.getAttribute('aria-expanded')!=='true';button.setAttribute('aria-expanded',expanded);button.textContent=expanded?'Thu gọn':'Xem thêm';measureComments();
}));
measureComments();window.addEventListener('resize',measureComments);

document.querySelectorAll('.comic-upload').forEach(form=>{
 const input=form.elements.pages,additional=form.querySelector('#comic-more-pages'),addButton=form.querySelector('.add-comic-pages'),preview=form.querySelector('.upload-preview'),status=form.querySelector('.upload-status');let files=[],urls=[];
 const render=()=>{
  urls.forEach(URL.revokeObjectURL);urls=[];preview.replaceChildren();
  const transfer=new DataTransfer();files.forEach(file=>transfer.items.add(file));input.files=transfer.files;
  form.elements.order.value=JSON.stringify(files.map((_,i)=>i));addButton.disabled=files.length>=50;
  files.forEach((file,i)=>{
   const item=document.createElement('div'),img=document.createElement('img'),label=document.createElement('p');
   const url=URL.createObjectURL(file);urls.push(url);img.src=url;img.alt='Xem trước trang '+(i+1);label.textContent=(i+1)+'. '+file.name;item.append(img,label);
   [['Lên',-1],['Xuống',1],['Bỏ',0]].forEach(([name,delta])=>{
    const button=document.createElement('button');button.type='button';button.className='button small';button.textContent=name;button.setAttribute('aria-label',name+' ảnh '+(i+1));button.disabled=delta<0&&i===0||delta>0&&i===files.length-1;
    button.addEventListener('click',()=>{if(delta===0)files.splice(i,1);else [files[i],files[i+delta]]=[files[i+delta],files[i]];render();const controls=preview.children[i+delta]?.querySelectorAll('button');(delta===0?addButton:controls?.[delta>0?1:0]??addButton).focus();});item.append(button);
   });preview.append(item);
  });status.textContent=files.length?files.length+' ảnh · '+(files.reduce((n,f)=>n+f.size,0)/1048576).toFixed(2)+' MiB':'';
 };
 const choose=(selected,append)=>{
  const candidate=append?[...files,...selected]:selected;
  if(candidate.length>50||candidate.some(f=>f.size>5*1048576||f.size===0||!/\.(jpe?g|png|webp)$/i.test(f.name))||candidate.reduce((n,f)=>n+f.size,0)>50*1048576){
   render();status.textContent='Chọn tối đa 50 ảnh JPG/PNG/WebP, không quá 5 MiB/ảnh và 50 MiB/chương. Danh sách cũ được giữ nguyên.';
  }else{files=candidate;render();}
  additional.value='';
 };
 input.addEventListener('change',()=>choose(Array.from(input.files),false));
 addButton.addEventListener('click',()=>additional.click());
 additional.addEventListener('change',()=>{choose(Array.from(additional.files),true);addButton.focus();});
 window.addEventListener('pagehide',()=>urls.forEach(URL.revokeObjectURL));
});

document.querySelectorAll('.search-popover').forEach(menu=>{
 const trigger=menu.querySelector('summary');
 menu.addEventListener('toggle',()=>{trigger.setAttribute('aria-expanded',menu.open);if(menu.open)menu.querySelector('input[name="q"]').focus();});
 menu.addEventListener('keydown',e=>{if(e.key==='Escape'){menu.open=false;trigger.focus();}});
 document.addEventListener('click',e=>{if(!menu.contains(e.target))menu.open=false;});
});

document.querySelectorAll('[data-ranking]').forEach(button=>button.addEventListener('click',()=>{
 document.querySelectorAll('[data-ranking]').forEach(b=>b.setAttribute('aria-pressed',b===button));
 document.querySelectorAll('.ranking-list').forEach(panel=>panel.hidden=panel.id!=='ranking-'+button.dataset.ranking);
}));

document.querySelectorAll('.file-upload').forEach(form=>{
 const input=form.elements.uploads,preview=form.querySelector('.upload-preview'),status=form.querySelector('.upload-status');let files=[];
 const render=()=>{
  const transfer=new DataTransfer();files.forEach(file=>transfer.items.add(file));input.files=transfer.files;
  form.elements.order.value=JSON.stringify(files.map((_,i)=>i));preview.replaceChildren();
  files.forEach((file,i)=>{
   const row=document.createElement('div'),label=document.createElement('p');label.textContent=`${i+1}. ${file.name}`;row.append(label);
   [['Lên',-1],['Xuống',1],['Bỏ',0]].forEach(([name,delta])=>{
    const button=document.createElement('button');button.type='button';button.className='button small';button.textContent=name;button.disabled=delta<0&&i===0||delta>0&&i===files.length-1;
    button.setAttribute('aria-label',`${name} tệp ${i+1}`);
    button.addEventListener('click',()=>{if(delta===0)files.splice(i,1);else [files[i],files[i+delta]]=[files[i+delta],files[i]];render();});row.append(button);
   });preview.append(row);
  });status.textContent=`${files.length} tệp · ${(files.reduce((n,f)=>n+f.size,0)/1048576).toFixed(2)} MiB`;
 };
 input.addEventListener('change',()=>{
  const candidate=Array.from(input.files),image=f=>/\.(jpe?g|png|webp)$/i.test(f.name);
  const valid=candidate.length<=50&&candidate.every(f=>f.size>0&&(/\.(txt|md)$/i.test(f.name)?f.size<=2*1048576:/\.pdf$/i.test(f.name)?f.size<=20*1048576:image(f)&&f.size<=5*1048576))&&candidate.reduce((n,f)=>n+f.size,0)<=100*1048576&&candidate.filter(image).reduce((n,f)=>n+f.size,0)<=50*1048576;
  if(valid){files=candidate;render();}else{render();status.textContent='Tệp vượt giới hạn hoặc sai loại. Danh sách trước được giữ nguyên.';}
 });
});
