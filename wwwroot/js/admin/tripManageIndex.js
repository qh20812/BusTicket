// Global variable for the map instance in the modal
let tripModalMap = null;

// Load environment variables
require('dotenv').config();

document.addEventListener('DOMContentLoaded', function () {
    // Always re-bind hidden input values on form submit (for ModelState error and modal re-show)
    const tripActionForm = document.getElementById('tripActionForm');
    if (tripActionForm) {
        tripActionForm.addEventListener('submit', function (e) {
            const tripIdInput = document.getElementById('tripIdToActionInputModal');
            const actionTypeInput = document.getElementById('actionTypeInputModal');
            if (typeof tripIdActionOnError !== 'undefined' && tripIdActionOnError) tripIdInput.value = tripIdActionOnError;
            if (typeof actionTypeOnError !== 'undefined' && actionTypeOnError) actionTypeInput.value = actionTypeOnError;
        });
    }
    // --- Existing JavaScript for tripActionModal ---
    const tripActionModalElement = document.getElementById('tripActionModal');
    if (tripActionModalElement) {
        var actionModal = new bootstrap.Modal(tripActionModalElement);
        tripActionModalElement.addEventListener('show.bs.modal', function (event) {
            const button = event.relatedTarget;
            const tripId = button.getAttribute('data-trip-id');
            const routeName = button.getAttribute('data-route-name');
            const actionType = button.getAttribute('data-action-type'); // "Cancel" or "Complete"
            const modalTitle = button.getAttribute('data-modal-title');

            tripActionModalElement.querySelector('#tripActionModalLabel').textContent = modalTitle;
            tripActionModalElement.querySelector('#routeNameToActionModal').textContent = routeName;
            tripActionModalElement.querySelector('#tripIdToActionTextModal').textContent = tripId;
            tripActionModalElement.querySelector('#tripIdToActionInputModal').value = tripId;
            tripActionModalElement.querySelector('#actionTypeInputModal').value = actionType;

            const cancellationReasonGroup = tripActionModalElement.querySelector('#cancellationReasonGroup');
            const confirmButton = tripActionModalElement.querySelector('#confirmActionButton');
            const actionNameModal = tripActionModalElement.querySelector('#actionNameModal');

            if (actionType === "Cancel") {
                actionNameModal.textContent = "hủy";
                cancellationReasonGroup.style.display = 'block';
                confirmButton.className = 'btn btn-warning'; // Changed to warning to match other cancel buttons
                confirmButton.textContent = 'Xác nhận Hủy';
            } else if (actionType === "Complete") {
                actionNameModal.textContent = "hoàn thành";
                cancellationReasonGroup.style.display = 'none';
                confirmButton.className = 'btn btn-success';
                confirmButton.textContent = 'Xác nhận Hoàn thành';
            }

            // Reset validation
            const form = document.getElementById('tripActionForm');
            form.reset(); // Reset các trường input
            const validationSpans = form.querySelectorAll('.text-danger');
            validationSpans.forEach(span => span.textContent = '');
            const reasonInput = form.querySelector('[name="TripActionInput.CancellationReason"]');
            if (reasonInput) reasonInput.value = ''; // Đảm bảo textarea được reset
        });
        if (typeof tripIdActionOnError !== 'undefined' && tripIdActionOnError && tripIdActionOnError !== '' && 
            typeof actionTypeOnError !== 'undefined' && actionTypeOnError && actionTypeOnError !== '') {
            
            var routeNameOnError = "ID " + tripIdActionOnError; // Fallback
            var modalTitleOnError = (actionTypeOnError === "Cancel" ? "Hủy chuyến đi" : "Hoàn thành chuyến đi");

            // Cố gắng tìm button để lấy thông tin chính xác hơn
            var buttonForError = document.querySelector(`.btn-action-modal[data-trip-id="${tripIdActionOnError}"][data-action-type="${actionTypeOnError}"]`);
            if (buttonForError) {
                routeNameOnError = buttonForError.getAttribute('data-route-name');
                modalTitleOnError = buttonForError.getAttribute('data-modal-title');
            }

            tripActionModalElement.querySelector('#tripActionModalLabel').textContent = modalTitleOnError;
            tripActionModalElement.querySelector('#routeNameToActionModal').textContent = routeNameOnError;
            tripActionModalElement.querySelector('#tripIdToActionTextModal').textContent = tripIdActionOnError;
            tripActionModalElement.querySelector('#tripIdToActionInputModal').value = tripIdActionOnError;
            tripActionModalElement.querySelector('#actionTypeInputModal').value = actionTypeOnError;

            const cancellationReasonGroupOnError = tripActionModalElement.querySelector('#cancellationReasonGroup');
            const confirmButtonOnError = tripActionModalElement.querySelector('#confirmActionButton');
            const actionNameModalOnError = tripActionModalElement.querySelector('#actionNameModal');

            if (actionTypeOnError === "Cancel") {
                actionNameModalOnError.textContent = "hủy";
                cancellationReasonGroupOnError.style.display = 'block';
                confirmButtonOnError.className = 'btn btn-warning';
                confirmButtonOnError.textContent = 'Xác nhận Hủy';
            } else if (actionTypeOnError === "Complete") {
                actionNameModalOnError.textContent = "hoàn thành";
                cancellationReasonGroupOnError.style.display = 'none';
                confirmButtonOnError.className = 'btn btn-success';
                confirmButtonOnError.textContent = 'Xác nhận Hoàn thành';
            }
            actionModal.show();
        }
    }
    const tripMapModalElement = document.getElementById('tripMapModal');
    if (tripMapModalElement) {
        tripMapModalElement.addEventListener('show.bs.modal', function (event) {
            const button = event.relatedTarget; // Button that triggered the modal
            const tripId = button.getAttribute('data-trip-id');
            const routeNameCell = button.closest('tr')?.querySelector('td:nth-child(2)');
            const tripRouteName = routeNameCell ? routeNameCell.textContent.trim() : `ID ${tripId}`;
            
            document.getElementById('tripMapModalLabel').textContent = `Lộ trình Chuyến đi: ${tripRouteName}`;
            
            // Initialize map when modal is fully shown to ensure container dimensions are correct
            const modalShownHandler = () => {
                initTripModalMap(tripId);
                tripMapModalElement.removeEventListener('shown.bs.modal', modalShownHandler); 
            };
            tripMapModalElement.addEventListener('shown.bs.modal', modalShownHandler);
        });

        tripMapModalElement.addEventListener('hidden.bs.modal', function () {
            if (tripModalMap) {
                tripModalMap.remove();
                tripModalMap = null;
            }
            document.getElementById('tripMapContainer').innerHTML = ''; // Clear map container
            document.getElementById('mapDistance').textContent = 'N/A';
            document.getElementById('mapDuration').textContent = 'N/A';
        });
    }

    // Attach event listeners to "Xem lộ trình trên bản đồ" buttons
    document.querySelectorAll('.view-trip-map-btn').forEach(button => {
        button.addEventListener('click', function () {
            // Bootstrap handles showing the modal via data-bs-toggle and data-bs-target.
            // Our 'show.bs.modal' and 'shown.bs.modal' listeners above will handle map initialization.
        });
    });

    const mapContainer = document.getElementById('tripMapContainer');
    const tempAlerts = document.querySelectorAll('.alert-dismissible.fade.show[role="alert"]');
    tempAlerts.forEach(function (alert) {
        setTimeout(function () {
            const alertInstance = bootstrap.Alert.getInstance(alert);
            if (alertInstance) { // Chỉ cần kiểm tra alertInstance tồn tại
                alertInstance.close();
            }
        }, 7000);
    });
});

async function initTripModalMap(tripId) {
    const mapContainer = document.getElementById('tripMapContainer');
    mapContainer.innerHTML = '<p class="text-center">Đang tải bản đồ...</p>';

    try {
        const response = await fetch(`/ForAdmin/TripManage/Index?handler=RouteDetailsForMap&tripId=${tripId}`);
        if (!response.ok) {
            let errorMsg = `Lỗi ${response.status}: ${response.statusText}`;
            try {
                const errorData = await response.json();
                errorMsg = errorData.value || errorData.title || errorMsg;
            } catch (e) {}
            mapContainer.innerHTML = `<p class="text-center text-danger">Lỗi tải dữ liệu lộ trình: ${errorMsg}</p>`;
            return;
        }
        const data = await response.json();
        mapContainer.innerHTML = '';
        if (tripModalMap) {
            tripModalMap.remove();
        }
        tripModalMap = L.map(mapContainer).setView([10.762622, 106.660172], 6);
        L.tileLayer('https://mt{s}.google.com/vt/lyrs=m&x={x}&y={y}&z={z}&key=' + process.env.GOOGLE_MAPS_API_KEY, {
            attribution: '&copy; Google',
            subdomains: ['0', '1', '2', '3']
        }).addTo(tripModalMap);
        const markers = [];
        let originCoordsTuple, destinationCoordsTuple;

        function addMarkerAndGetLatLng(coordsStr, title) {
            if (coordsStr) {
                const [lat, lon] = coordsStr.split(',').map(Number);
                if (!isNaN(lat) && !isNaN(lon)) {
                    const marker = L.marker([lat, lon]).addTo(tripModalMap).bindPopup(title);
                    markers.push(marker.getLatLng());
                    return [lat, lon];
                }
            }
            return null;
        }
        if (data.originCoordinates && data.destinationCoordinates) {
            originCoordsTuple = addMarkerAndGetLatLng(data.originCoordinates, `Điểm đi: ${data.originAddress || 'N/A'}`);
            destinationCoordsTuple = addMarkerAndGetLatLng(data.destinationCoordinates, `Điểm đến: ${data.destinationAddress || 'N/A'}`);
        } else {
            mapContainer.innerHTML = `<p class="text-center text-warning">Không có tọa độ cho lộ trình này. Không thể vẽ bản đồ chi tiết.</p>`;
            return;
        }
        if (originCoordsTuple && destinationCoordsTuple) {
            L.polyline([originCoordsTuple, destinationCoordsTuple], { color: 'blue' }).addTo(tripModalMap);
        }
        if (markers.length > 0) {
            tripModalMap.fitBounds(markers, { padding: [50, 50] });
        } else if (data.originAddress && data.destinationAddress && !(data.originCoordinates && data.destinationCoordinates)) {
            mapContainer.innerHTML = `<p class="text-center text-warning">Lộ trình từ <strong>${data.originAddress}</strong> đến <strong>${data.destinationAddress}</strong> không có tọa độ chi tiết để vẽ bản đồ.</p>`;
        } else {
            tripModalMap.setView([10.762622, 106.660172], 6);
        }
        document.getElementById('mapDistance').textContent = data.distance ? `${data.distance} km` : 'N/A (Cần dịch vụ định tuyến)';
        document.getElementById('mapDuration').textContent = data.estimatedDuration || 'N/A (Cần dịch vụ định tuyến)';
    } catch (error) {
        console.error('Lỗi khi tải bản đồ:', error);
        mapContainer.innerHTML = `<p class="text-center text-danger">Lỗi khi tải bản đồ. ${error.message}</p>`;
    }
}