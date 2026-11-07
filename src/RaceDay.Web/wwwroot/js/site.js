// RaceDay client-side helpers. The pages work without JavaScript; this only adds small conveniences.

// Preview an image before it is uploaded (banner and profile picture forms).
document.querySelectorAll('input[type="file"][data-preview]').forEach(input => {
    input.addEventListener('change', () => {
        const target = document.getElementById(input.dataset.preview);
        const file = input.files && input.files[0];
        if (!target || !file || !file.type.startsWith('image/')) {
            return;
        }
        const url = URL.createObjectURL(file);
        if (target.tagName === 'IMG') {
            target.src = url;
            target.classList.remove('d-none');
            document.querySelectorAll('[data-initials]').forEach(el => el.remove());
        } else {
            target.style.backgroundImage = `url("${url}")`;
            target.classList.remove('rd-banner-placeholder');
            target.innerHTML = '';
        }
    });
});

// Show the role-specific fields on the registration form for the selected role.
const roleInputs = document.querySelectorAll('input[name="Role"]');
function syncRoleFields() {
    const selected = document.querySelector('input[name="Role"]:checked');
    document.querySelectorAll('[data-role-fields]').forEach(group => {
        group.hidden = !selected || group.dataset.roleFields !== selected.value;
    });
}
roleInputs.forEach(input => input.addEventListener('change', syncRoleFields));
syncRoleFields();
