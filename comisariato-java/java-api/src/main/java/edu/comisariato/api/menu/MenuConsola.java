package edu.comisariato.api.menu;
import java.math.BigDecimal;
import java.net.URI;
import java.net.http.*;
import java.time.Duration;
import java.util.Scanner;
/** Cliente independiente de consola. Ejecutar con Java 17+ en modo archivo fuente. */
public class MenuConsola {
    private static final Scanner in = new Scanner(System.in);
    private static final HttpClient http = HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(10)).build();
    public static void main(String[] args) {
        String base = args.length > 0 ? args[0].replaceAll("/+$", "") : "http://localhost:8080";
        while (true) {
            System.out.println("\n=== COMISARIATO | JAVA ===\n1. Registrar venta\n2. Anular venta\n3. Productos con stock bajo\n4. Ventas por empleado\n5. Movimientos por producto\n0. Salir");
            if (!in.hasNextLine()) return;
            String option = ask("Opcion: ");
            try {
                switch(option) {
                    case "1" -> send(base + "/api/ventas", venta());
                    case "2" -> {
                        int id = positive("Venta ID: ");
                        if (ask("Confirmar anulacion (s/n): ").equalsIgnoreCase("s")) send(base+"/api/ventas/"+id+"/anular", "");
                    }
                    case "3" -> send(base+"/api/productos/stock-bajo", null);
                    case "4" -> send(base+"/api/empleados/"+positive("Empleado ID: ")+"/ventas", null);
                    case "5" -> send(base+"/api/productos/"+positive("Producto ID: ")+"/movimientos", null);
                    case "0" -> { return; }
                    default -> System.out.println("Opcion invalida.");
                }
            } catch (InterruptedException e) { Thread.currentThread().interrupt(); return; }
            catch (java.util.NoSuchElementException e) { return; }
            catch (Exception e) { System.out.println("No se pudo ejecutar: " + e.getMessage()); }
        }
    }
    private static String venta() {
        String factura = ask("Numero de factura (max. 20, letras/numeros/guion): ");
        if (!factura.matches("[A-Za-z0-9-]{1,20}")) throw new IllegalArgumentException("Factura invalida");
        String cliente = ask("Cliente ID (Enter = consumidor final): ");
        if (!cliente.isEmpty() && Integer.parseInt(cliente)<=0) throw new IllegalArgumentException("Cliente invalido");
        int empleado = positive("Empleado ID: ");
        String pago = ask("Pago: 1 Efectivo, 2 Tarjeta, 3 Transferencia: ");
        pago = switch(pago) { case "1" -> "Efectivo"; case "2" -> "Tarjeta"; case "3" -> "Transferencia"; default -> throw new IllegalArgumentException("Pago invalido"); };
        int count = positive("Numero de productos distintos (1-100): ");
        if (count>100) throw new IllegalArgumentException("Maximo 100 productos");
        StringBuilder items = new StringBuilder();
        for(int i=0;i<count;i++) {
            int id = positive("Producto ID: ");
            int cantidad = positive("Cantidad: ");
            String value = ask("Descuento TOTAL de esta linea (Enter = 0): ");
            BigDecimal discount = new BigDecimal(value.isEmpty()?"0":value);
            if (discount.signum()<0 || discount.scale()>2) throw new IllegalArgumentException("Descuento invalido");
            if(i>0) items.append(',');
            items.append("{\"productoId\":").append(id).append(",\"cantidad\":").append(cantidad)
                .append(",\"descuento\":").append(discount.toPlainString()).append('}');
        }
        return "{\"numeroFactura\":\""+factura+"\",\"clienteId\":"+(cliente.isEmpty()?"null":Integer.parseInt(cliente))+
            ",\"empleadoId\":"+empleado+",\"formaPago\":\""+pago+"\",\"detalles\":["+items+"]}";
    }
    private static void send(String url, String body) throws Exception {
        var builder = HttpRequest.newBuilder(URI.create(url)).timeout(Duration.ofSeconds(90));
        if(body==null) builder.GET();
        else builder.header("Content-Type","application/json").POST(HttpRequest.BodyPublishers.ofString(body));
        var result = http.send(builder.build(), HttpResponse.BodyHandlers.ofString());
        System.out.println("HTTP " + result.statusCode() + "\n" + result.body());
    }
    private static String ask(String prompt) { System.out.print(prompt); return in.nextLine().trim(); }
    private static int positive(String prompt) {
        int number = Integer.parseInt(ask(prompt));
        if (number<=0) throw new IllegalArgumentException("El ID o cantidad debe ser positivo");
        return number;
    }
}
