// Drag-drop interop helpers for template editor

window.dragDropInterop = {
    draggedId: null,
    
    setDraggedId: function(id) {
        console.log('[DragDrop] setDraggedId:', id);
        window.dragDropInterop.draggedId = id;
    },
    
    getDraggedId: function() {
        console.log('[DragDrop] getDraggedId:', window.dragDropInterop.draggedId);
        return window.dragDropInterop.draggedId;
    },
    
    clearDraggedId: function() {
        console.log('[DragDrop] clearDraggedId');
        window.dragDropInterop.draggedId = null;
    }
};

// Log drag events for debugging
document.addEventListener('dragstart', function(e) {
    console.log('[DragEvent] dragstart on:', e.target, 'dataTransfer:', e.dataTransfer);
}, true);

document.addEventListener('dragover', function(e) {
    // Allow drop
    if (e.preventDefault) {
        e.preventDefault();
    }
    e.dataTransfer.dropEffect = 'move';
}, true);

document.addEventListener('drop', function(e) {
    console.log('[DragEvent] drop on:', e.target, 'draggedId:', window.dragDropInterop.draggedId);
}, true);
